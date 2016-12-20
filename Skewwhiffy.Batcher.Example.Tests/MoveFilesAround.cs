using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Skewwhiffy.Batcher.Extensions;
using Skewwhiffy.Batcher.Fluent;

namespace Skewwhiffy.Batcher.Example.Tests
{
    public class MoveFilesAround
    {
        private const int NumberOfFiles = 1000;
        private const int CreateFileThreads = 3;
        private const int MoveFileThreads = 4;
        private const int MungeFileThreads = 5;
        private readonly object _lock = new object();
        private volatile string _sandbox;
        private volatile string _moved;
        private volatile string _munged;
        private List<List<TaskMetadata>> _results;

        private string Sandbox
        {
            get
            {
                if (_sandbox != null)
                {
                    return _sandbox;
                }
                lock (_lock)
                {
                    if (_sandbox != null)
                    {
                        return _sandbox;
                    }
                    var pwd = Environment.CurrentDirectory;
                    var sandbox = Path.Combine(pwd, "__");
                    if (Directory.Exists(sandbox))
                    {
                        Directory.Delete(sandbox, true);
                    }
                    Directory.CreateDirectory(sandbox);
                    _sandbox = sandbox;
                }
                return _sandbox;
            }
        }

        private string Moved
        {
            get
            {
                if (_moved != null)
                {
                    return _moved;
                }
                lock (_lock)
                {
                    if (_moved != null)
                    {
                        return _moved;
                    }
                    var moved = Path.Combine(Sandbox, "Moved");
                    Directory.CreateDirectory(moved);
                    _moved = moved;
                }
                return _moved;
            }
        }

        private string Munged
        {
            get
            {
                if (_munged != null)
                {
                    return _munged;
                }
                lock (_lock)
                {
                    if (_munged != null)
                    {
                        return _munged;
                    }
                    var munged = Path.Combine(Sandbox, "Munged");
                    Directory.CreateDirectory(munged);
                    _munged = munged;
                }
                return _munged;
            }
        }

        private string GetFilename(int i)
        {
            return $"{i}".PadLeft($"{NumberOfFiles}".Length, '0') + ".json";
        }

        [OneTimeSetUp]
        public async Task BeforeAll()
        {
            var batcher = TakeAnItem<int>
                .Then(CreateFile)
                .WithThreads(CreateFileThreads)
                .Then(MoveFile)
                .WithThreads(MoveFileThreads)
                .AndFinally(MungeFile)
                .WithThreads(MungeFileThreads);
            batcher.Process(1.To(NumberOfFiles));

            var logNext = 0;
            while (true)
            {
                var count = Moved.Pipe(Directory.GetFiles).Pipe(f => f.Length);
                if (count >= logNext)
                {
                    Console.WriteLine($"{count} files moved");
                    logNext += NumberOfFiles / 10;
                }
                if (count >= NumberOfFiles)
                {
                    break;
                }
                await Task.Delay(100);
            }
            Console.WriteLine("**** Files moved ****");
            logNext = 0;
            while (true)
            {
                var count = Munged.Pipe(Directory.GetFiles).Pipe(f => f.Length);
                if (count >= logNext)
                {
                    Console.WriteLine($"{count} files munged");
                    logNext += NumberOfFiles / 10;
                }
                if (count >= NumberOfFiles)
                {
                    break;
                }
                await Task.Delay(100);
            }
            Console.WriteLine("**** Files munged ****");
            _results = 1.To(NumberOfFiles)
                .Select(GetFilename)
                .Select(f => Path.Combine(Munged, f))
                .Select(File.ReadAllText)
                .Select(JsonConvert.DeserializeObject<List<TaskMetadata>>)
                .ToList();
        }

        [Test]
        public void AllFilesAreMunged()
        {
            Assert.That(_results.Count, Is.EqualTo(NumberOfFiles));
        }

        [Test]
        public void FileMungeIsMultiThreaded()
        {
            var threadIds = _results.Select(r => r[1]).Select(tm => tm.ThreadId).Distinct().ToList();

            Assert.That(threadIds.Count, Is.EqualTo(MungeFileThreads));
        }

        [Test]
        public void CreateFileIsMultiThreaded()
        {
            var threadIds = _results.Select(r => r[0]).Select(tm => tm.ThreadId).Distinct().ToList();

            Assert.That(threadIds.Count, Is.EqualTo(CreateFileThreads));
        }

        private async Task<string> CreateFile(int i)
        {
            var filename = GetFilename(i);
            var fullFilename = Path.Combine(Sandbox, filename);
            var payload = new TaskMetadata
            {
                ThreadId = Thread.CurrentThread.ManagedThreadId
            };
            using (var writer = File.CreateText(fullFilename))
            {
                await payload
                    .Pipe(JsonConvert.SerializeObject)
                    .Pipe(writer.WriteAsync);
            }
            return filename;
        }

        private string MoveFile(string file)
        {
            var originalFilename = Path.Combine(Sandbox, file);
            var newFilename = Path.Combine(Moved, file);
            File.Move(originalFilename, newFilename);
            return file;
        }

        private async Task MungeFile(string file)
        {
            var fileName = Path.Combine(Moved, file);
            var newFilename = Path.Combine(Munged, file);
            var payload = File
                .ReadAllText(fileName)
                .Pipe(JsonConvert.DeserializeObject<TaskMetadata>);
            var newPayload = new List<TaskMetadata>
            {
                payload,
                new TaskMetadata
                {
                    ThreadId = Thread.CurrentThread.ManagedThreadId
                }
            };
            using (var stream = File.CreateText(newFilename))
            {
                await newPayload
                    .Pipe(JsonConvert.SerializeObject)
                    .Pipe(stream.WriteAsync);
            }
        }
    }
}
