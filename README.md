# Batcher
A framework in .NET for executing large batch jobs.

## History
Batcher was written by K Hung in C# originally in 2016. It was inspired by a need to back up a large NoSQL database with many million records to another data store (which was slower to access, but more reliable).

The original problem was solved by backing up the DB to disk, then taking that disk and backing this up to the final data store, taking care to make both of those steps recoverable. However, the main problem was that the original scan itself, and the backing up to the data store needed to be parallelized so as to minimize the amount of time needed to complete the task.

## Usage
An example use can be found [here](https://github.com/skewwhiffy/Batcher/blob/master/Skewwhiffy.Batcher.Example.Tests/MoveFilesAround.cs), and reproduced below.
```cs
var batcher = TakeAnItem<int>
    .Then(CreateFile)
    .WithThreads(CreateFileThreads)
    .Then(MoveFile)
    .WithThreads(MoveFileThreads)
    .AndFinally(MungeFile)
    .WithThreads(MungeFileThreads);
batcher.Process(1.To(NumberOfFiles));
```

* Threads and processing will only start when the `.Process()` method is invoked.
* Processing will continue until the `batcher` is disposed (which sends a cancellation request to all running tasks).
* Exceptions in functions are exposed as the `ExceptionEvent` event on the batcher.
* Exceptions are aggregated in a `List<Exception>` property called `Exceptions` on the batcher.
