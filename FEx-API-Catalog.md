# FEx Framework API Catalog

**Generated:** 2025-10-26 17:06:35  
**Version:** 1.0

> 🤖 **AI Agent Usage:** Load this file into context when working with projects that reference FEx framework.

---

## 📋 Table of Contents
- [FEx.Agnostics](#fexagnostics)
- [FEx.Agnostics.Abstractions](#fexagnosticsabstractions)
- [FEx.Agnostics.TestMocks](#fexagnosticstestmocks)
- [FEx.AppSettings](#fexappsettings)
- [FEx.Asyncx](#fexasyncx)
- [FEx.Avaloniax](#fexavaloniax)
- [FEx.AzureStorage](#fexazurestorage)
- [FEx.CLI](#fexcli)
- [FEx.Common](#fexcommon)
- [FEx.Common.Abstractions](#fexcommonabstractions)
- [FEx.Core](#fexcore)
- [FEx.Core.Abstractions](#fexcoreabstractions)
- [FEx.DependencyInjection](#fexdependencyinjection)
- [FEx.DependencyInjection.Abstractions](#fexdependencyinjectionabstractions)
- [FEx.Downloader](#fexdownloader)
- [FEx.EFCore](#fexefcore)
- [FEx.Encryption](#fexencryption)
- [FEx.FileSystem](#fexfilesystem)
- [FEx.Flurlx](#fexflurlx)
- [FEx.FTPx](#fexftpx)
- [FEx.Json](#fexjson)
- [FEx.KeyVault](#fexkeyvault)
- [FEx.Legacy](#fexlegacy)
- [FEx.Logging](#fexlogging)
- [FEx.Logging.Abstractions](#fexloggingabstractions)
- [FEx.Maui](#fexmaui)
- [FEx.MVVM](#fexmvvm)
- [FEx.MVVM.Abstractions](#fexmvvmabstractions)
- [FEx.MVVM.Rx](#fexmvvmrx)
- [FEx.NuGetx](#fexnugetx)
- [FEx.OneDrv](#fexonedrv)
- [FEx.PersistentStorage](#fexpersistentstorage)
- [FEx.PersistentStorage.Abstractions](#fexpersistentstorageabstractions)
- [FEx.PersistentStorage.Rx](#fexpersistentstoragerx)
- [FEx.Platforms](#fexplatforms)
- [FEx.Platforms.Windows](#fexplatformswindows)
- [FEx.RESXx](#fexresxx)
- [FEx.SecureStorage](#fexsecurestorage)
- [FEx.Telemetry](#fextelemetry)
- [FEx.Telemetry.Rollbar](#fextelemetryrollbar)
- [FEx.WebScraping](#fexwebscraping)
- [FEx.Webx](#fexwebx)
- [FEx.WPFx](#fexwpfx)

---

## FEx.Agnostics

**Namespace:** `Flakroup.FEx.Agnostics`  
**Classes:** 27 | **Interfaces:** 2 | **Enums:** 0
**Extension Methods:** 12 | **Methods:** 24 | **Properties:** 7

### 🔌 Extension Methods

#### Extensions for `AsyncReaderWriterLockSlim`

- **`DowngradeWriteLockToReadLock`** → `void`
  - Downgrades the lock from write mode to read mode.
  - `void DowngradeWriteLockToReadLock(this AsyncReaderWriterLockSlim lockInstance,
                                                    IDisposableLock readLock)`
  - 📁 AsyncReaderWriterLockSlimExtension.cs

- **`GetReadLock`** → `IDisposableLock`
  - Enters the lock in read mode.
  - `IDisposableLock GetReadLock(this AsyncReaderWriterLockSlim lockInstance,
                                              CancellationToken cancellationToken = default)`
  - 📁 AsyncReaderWriterLockSlimExtension.cs

- **`GetWriteLock`** → `IDisposableLock`
  - Enters the lock in write mode.
  - `IDisposableLock GetWriteLock(this AsyncReaderWriterLockSlim lockInstance,
                                               CancellationToken cancellationToken = default)`
  - 📁 AsyncReaderWriterLockSlimExtension.cs

- **`TryGetReadLock`** → `IDisposableLock`
  - Tries to enter the lock in read mode, with an optional integer time-out.
  - `IDisposableLock TryGetReadLock(this AsyncReaderWriterLockSlim lockInstance,
                                                 int millisecondsTimeout,
                                                 CancellationToken cancellationToken = default)`
  - 📁 AsyncReaderWriterLockSlimExtension.cs

- **`TryGetWriteLock`** → `IDisposableLock`
  - Tries to enter the lock in write mode, with an optional integer time-out.
  - `IDisposableLock TryGetWriteLock(this AsyncReaderWriterLockSlim lockInstance,
                                                  int millisecondsTimeout,
                                                  CancellationToken cancellationToken = default)`
  - 📁 AsyncReaderWriterLockSlimExtension.cs

#### Extensions for `FileInfo`

- **`FindFileEmulator`** → `FileInfo`
  - `FileInfo FindFileEmulator(this FileInfo file, params string[] patterns)`
  - 📁 FindFilesPatternToRegex.cs

#### Extensions for `IEnumerable<FileInfo>`

- **`FindFilesEmulator`** → `IEnumerable<FileInfo>`
  - `IEnumerable<FileInfo> FindFilesEmulator(this IEnumerable<FileInfo> files, params string[] patterns)`
  - 📁 FindFilesPatternToRegex.cs

#### Extensions for `IEnumerable<string>`

- **`OrderAlphanumBy`** → `IOrderedEnumerable<string>`
  - `IOrderedEnumerable<string> OrderAlphanumBy(this IEnumerable<string> source)`
  - 📁 EnumerableExtensions.cs

- **`OrderAlphanumByDescending`** → `IOrderedEnumerable<string>`
  - `IOrderedEnumerable<string> OrderAlphanumByDescending(this IEnumerable<string> source)`
  - 📁 EnumerableExtensions.cs

#### Extensions for `string`

- **`FindFileEmulator`** → `string`
  - `string FindFileEmulator(this string pattern, string name)`
  - 📁 FindFilesPatternToRegex.cs

- **`FindFilesEmulator`** → `IEnumerable<string>`
  - `IEnumerable<string> FindFilesEmulator(this string pattern, params string[] names)`
  - 📁 FindFilesPatternToRegex.cs

- **`PathHasIllegalCharacters`** → `bool`
  - `bool PathHasIllegalCharacters(this string pattern)`
  - 📁 FindFilesPatternToRegex.cs

### 🔷 Interfaces

- **`ICmd`**
- **`IDisposableLock`** - Downgrades the lock from write mode to read mode.

### 📦 Classes

- **`AlphanumComparatorFast`**
- **`AsyncReaderWriterLockSlim`**
- **`AsyncReaderWriterLockSlimExtension`** - Contains extension methods for .
- **`Cmd`**
- **`CollectionDebugView`**
- **`ColorHelper`**
- **`ConcurrentSortableList`**
- **`DeepCopyByExpressionTrees`** - Superfast deep copier class, which uses Expression trees.
- **`DefaultAppVersionProvider`**
- **`EnumerableExtensions`**
- **`ExtendedReaderWriterLockSlim`**
- **`ExtendedStringWriter`**
- **`FExException`**
- **`FExMemoryCache`**
- **`FExSingleton`**
- **`FExSingleton`**
- **`FindFilesPatternToRegex`**
- **`FlakDynamicObject`**
- **`InterlockedBool`**
- **`ListDebugView`**
- **`ListExtensions`**
- **`NonSpaceIgnoringStringComparer`**
- **`NotifyPropertyChanged`**
- **`PaginatedList`**
- **`PropertyChangeAware`**
- **`StringHelpers`** *(static)*
- **`SummarizedPaginatedList`**

### ⚙️ Public Methods

#### 📁 AlphanumComparatorFast.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode(string obj)`

#### 📁 AsyncReaderWriterLockSlim.cs (12 methods)

- **`Dispose`** → `void`
  - Releases all resources used by the .
  - `void Dispose()`

- **`DowngradeWriteLockToReadLock`** → `void`
  - Downgrades the lock from write mode to read mode.
  - `void DowngradeWriteLockToReadLock()`

- **`EnterReadLock`** → `void`
  - Enters the lock in read mode.
  - `void EnterReadLock(CancellationToken cancellationToken = default)`

- **`EnterReadLockAsync`** → `Task`
  - Asynchronously enters the lock in read mode.
  - `Task EnterReadLockAsync(CancellationToken cancellationToken = default)`

- **`EnterWriteLock`** → `void`
  - Enters the lock in write mode.
  - `void EnterWriteLock(CancellationToken cancellationToken = default)`

- **`EnterWriteLockAsync`** → `Task`
  - Asynchronously enters the lock in write mode.
  - `Task EnterWriteLockAsync(CancellationToken cancellationToken = default)`

- **`ExitReadLock`** → `void`
  - Exits read mode.
  - `void ExitReadLock()`

- **`ExitWriteLock`** → `void`
  - Exits write mode.
  - `void ExitWriteLock()`

- **`TryEnterReadLock`** → `bool`
  - Tries to enter the lock in read mode, with an optional integer time-out.
  - `bool TryEnterReadLock(int millisecondsTimeout = 0, CancellationToken cancellationToken = default)`

- **`TryEnterReadLockAsync`** → `Task<bool>`
  - Tries to asynchronously enter the lock in read mode, with an optional integer time-out.
  - `Task<bool> TryEnterReadLockAsync(int millisecondsTimeout = 0,
                                                  CancellationToken cancellationToken = default)`

- **`TryEnterWriteLock`** → `bool`
  - Tries to enter the lock in write mode, with an optional integer time-out.
  - `bool TryEnterWriteLock(int millisecondsTimeout = 0, CancellationToken cancellationToken = default)`

- **`TryEnterWriteLockAsync`** → `Task<bool>`
  - Tries to asynchronously enter the lock in write mode, with an optional integer time-out.
  - `Task<bool> TryEnterWriteLockAsync(int millisecondsTimeout = 0,
                                                   CancellationToken cancellationToken = default)`

#### 📁 ColorHelper.cs

- **`FromArgbString`** → `Color`
  - Gets from ARGB string.
  - `Color FromArgbString(string colorcode)`

- **`FromRgbString`** → `Color`
  - Froms the RGB string.
  - `Color FromRgbString(string colorcode)`

#### 📁 ConcurrentList.cs

- **`GetEnumerator`** → `IEnumerator<T>`
  - `IEnumerator<T> GetEnumerator()`

#### 📁 DeepCopyByExpressionTrees.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode(object obj)`

#### 📁 DefaultAppVersionProvider.cs

- **`GetAppVersion`** → `string`
  - `string GetAppVersion()`

#### 📁 FindFilesPatternToRegex.cs

- **`Convert`** → `Regex`
  - `Regex Convert(string pattern)`

#### 📁 FlakDynamicObject.cs

- **`GetDynamicMemberNames`** → `IEnumerable<string>`
  - Returns the enumeration of all dynamic member names.
  - `IEnumerable<string> GetDynamicMemberNames()`

- **`TryGetMember`** → `bool`
  - Provides the implementation for operations that get member values. Classes derived from the class can override this method to specify dynamic behavior for operations such as getting a value for a property.
  - `bool TryGetMember(GetMemberBinder binder, out object result)`

- **`TrySetMember`** → `bool`
  - Provides the implementation for operations that set member values. Classes derived from the class can override this method to specify dynamic behavior for operations such as setting a value for a property.
  - `bool TrySetMember(SetMemberBinder binder, object value)`

#### 📁 InterlockedBool.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

#### 📁 NonSpaceIgnoringStringComparer.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode(string obj)`

### 📊 Properties

- **`LockOrigin`** : `AsyncReaderWriterLockSlim`
  - Downgrades the lock from write mode to read mode.
  - 📁 AsyncReaderWriterLockSlimExtension.cs

- **`ReadLockReleaseSemaphoreReleased`** : `bool`
  - Gets or sets a value that indicates if a read lock that is exited when there is a write lock present should not release the as it has already been released (or there were no read locks present when the write lock was initially entered).
  - 📁 AsyncReaderWriterLockSlim.cs

- **`StateIsActive`** : `bool`
  - Gets or sets a value that indicates if the state is active. Only when true, the will be released once the last read lock exits.
  - 📁 AsyncReaderWriterLockSlim.cs

- **`StateIsReleased`** : `bool`
  - Gets or sets a value that indicates if the write lock associated with this has already been released. This is also used to indicate if the the task or thread that waits on the semaphore and then decrements to zero (0) must dispose the semaphore.
  - 📁 AsyncReaderWriterLockSlim.cs

- **`WaitForReadLocks`** : `bool`
  - Gets or sets a value that indicates if a write lock that uses an existing must wait until the is released.
  - 📁 AsyncReaderWriterLockSlim.cs

- **`WaitingReadLocksCount`** : `int`
  - Gets or sets a value that indicates the number of tasks or threads which intend to wait on the semaphore. This is used to determine which task or thread is responsible to dispose the if is true.
  - 📁 AsyncReaderWriterLockSlim.cs

- **`WaitingReadLocksSemaphore`** : `SemaphoreSlim`
  - Gets or sets a on which new read locks need to wait until the existing write lock is released. The will be created only if there is at least on additional task or thread that wants to enter a read lock.
  - 📁 AsyncReaderWriterLockSlim.cs

---

## FEx.Agnostics.Abstractions

**Namespace:** `Flakroup.FEx.AgnosticsAbstractions`  
**Classes:** 75 | **Interfaces:** 24 | **Enums:** 7
**Extension Methods:** 225 | **Methods:** 36 | **Properties:** 18

### 🔌 Extension Methods

#### Extensions for `Action`

- **`Wrap`** → `object`
  - `object Wrap(this Action action)`
  - 📁 TaskExtensions.cs

#### Extensions for `AggregatedError`

- **`ToAggregateException`** → `AggregateException`
  - `AggregateException ToAggregateException(this AggregatedError error)`
  - 📁 ErrorExtensions.cs

#### Extensions for `Array`

- **`IndexOf`** → `int`
  - Indexes the of.
  - `int IndexOf(this Array source, object value)`
  - 📁 ArrayExtensions.cs

- **`WithinIndex`** → `bool`
  - Check if the index is within the array
  - `bool WithinIndex(this Array source, int index)`
  - 📁 ArrayExtensions.cs

#### Extensions for `AsyncOptions`

- **`HasFlagFast`** → `bool`
  - `bool HasFlagFast(this AsyncOptions value, AsyncOptions flag)`
  - 📁 AsyncOptionsExtensions.cs

#### Extensions for `bool`

- **`AndAlso`** → `bool`
  - Combines 2 boolean (And operation).
  - `bool AndAlso(this bool first, bool second)`
  - 📁 BooleanExtensions.cs

- **`AndNot`** → `bool`
  - Combines 2 boolean (And Not operation).
  - `bool AndNot(this bool first, bool second)`
  - 📁 BooleanExtensions.cs

- **`IfFalse`** → `void`
  - Execute action if value is False.
  - `void IfFalse(this bool value, Action action)`
  - 📁 BooleanExtensions.cs

- **`IfTrue`** → `void`
  - Execute action if value is True.
  - `void IfTrue(this bool value, Action action)`
  - 📁 BooleanExtensions.cs

- **`IfTrueOrFalse`** → `void`
  - Execute action if value is True/False.
  - `void IfTrueOrFalse(this bool value, Action actionTrue, Action actionFalse)`
  - 📁 BooleanExtensions.cs

- **`IsFalse`** → `bool`
  - Gets a value indicating if the value is False
  - `bool IsFalse(this bool value)`
  - 📁 BooleanExtensions.cs

- **`OrElse`** → `bool`
  - Combines 2 boolean (Or operation).
  - `bool OrElse(this bool first, bool second)`
  - 📁 BooleanExtensions.cs

- **`ThrowArgumentOutOfRange`** → `void`
  - Checks an condition for True/False.
  - `void ThrowArgumentOutOfRange(this bool condition, string argumentName)`
  - 📁 BooleanExtensions.cs

- **`ToInteger`** → `int`
  - Gets a int from a boolean.
  - `int ToInteger(this bool value)`
  - 📁 BooleanExtensions.cs

#### Extensions for `bool?`

- **`ToBooleanSafe`** → `bool`
  - Converts the nullable boolean to a boolean.
  - `bool ToBooleanSafe(this bool? value)`
  - 📁 BooleanExtensions.cs

- **`ToBooleanSafe`** → `bool`
  - Converts the nullable boolean to a boolean.
  - `bool ToBooleanSafe(this bool? value, bool defaultValue)`
  - 📁 BooleanExtensions.cs

- **`ToStringSafe`** → `string`
  - Converts the nullable boolean to a string.
  - `string ToStringSafe(this bool? value)`
  - 📁 BooleanExtensions.cs

#### Extensions for `byte[]`

- **`ByteArrayToString`** → `string`
  - `string ByteArrayToString(this byte[] ba)`
  - 📁 StringExtensions.cs

- **`ComputeMd5Hash`** → `string`
  - Computes the md5 hash.
  - `string ComputeMd5Hash(this byte[] data,
                                        bool removeDashes = true,
                                        bool toLower = true,
                                        bool asBase64String = false)`
  - 📁 FileInfoExtensions.cs

- **`GetHashString`** → `string`
  - `string GetHashString(this byte[] hash,
                                       bool removeDashes = true,
                                       bool toLower = true,
                                       bool asBase64String = false)`
  - 📁 HashHelper.cs

#### Extensions for `DateTime`

- **`ClearMilliseconds`** → `DateTime`
  - Clears the milliseconds.
  - `DateTime ClearMilliseconds(this DateTime dateTime)`
  - 📁 DateTimeExtensions.cs

- **`ConvertDateBetweenTimeZones`** → `DateTime`
  - Gets a value indicating whether a day is after or equal a specified date.
  - `DateTime ConvertDateBetweenTimeZones(this DateTime date,
                                                       TimeZoneInfo timeZoneFrom,
                                                       TimeZoneInfo timeZoneTo)`
  - 📁 DateTimeExtensions.cs

- **`ConvertLocalToTimeZone`** → `DateTime`
  - Gets a value indicating whether a day is after or equal a specified date.
  - `DateTime ConvertLocalToTimeZone(this DateTime dateTime, TimeZoneInfo timeZone)`
  - 📁 DateTimeExtensions.cs

- **`DateDiff`** → `double`
  - Returns a specifying the number of time intervals between two values.
  - `double DateDiff(this DateTime date1,
                                  DateTime date2,
                                  DateInterval interval,
                                  DayOfWeek? dayOfWeek = null)`
  - 📁 DateTimeExtensions.cs

- **`DateDiffIsBetween`** → `bool`
  - Compares the difference between and in the specified interval, and returns whether this difference is between the bounds given.
  - `bool DateDiffIsBetween(this DateTime date1,
                                         DateTime date2,
                                         DateInterval interval,
                                         double min,
                                         double max,
                                         DayOfWeek? dayOfWeek = null)`
  - 📁 DateTimeExtensions.cs

- **`GetCountDaysOfMonth`** → `int`
  - Gets the number of days in the month of the provided date.
  - `int GetCountDaysOfMonth(this DateTime current)`
  - 📁 DateTimeExtensions.cs

- **`GetDayOfWeek`** → `DayOfWeek`
  - `DayOfWeek GetDayOfWeek(this DateTime dt, DayOfWeek? weekdayFirst = null)`
  - 📁 DateTimeExtensions.cs

- **`GetLastDayOfMonth`** → `DateTime`
  - Gets the date of last day of month.
  - `DateTime GetLastDayOfMonth(this DateTime date)`
  - 📁 DateTimeExtensions.cs

- **`GetLocalizedDateString`** → `string`
  - `string GetLocalizedDateString(this DateTime date, string format = "dd.MM.yyyy")`
  - 📁 DateTimeExtensions.cs

- **`GetLocalizedFullDateTimeString`** → `string`
  - `string GetLocalizedFullDateTimeString(this DateTime date, string format = "dd.MM.yyyy HH:mm")`
  - 📁 DateTimeExtensions.cs

- **`GetLocalizedShortTimeString`** → `string`
  - `string GetLocalizedShortTimeString(this DateTime date, CultureInfo culture)`
  - 📁 DateTimeExtensions.cs

- **`GetStartOfWeek`** → `DateTime`
  - Gets a value indicating whether a day is after or equal a specified date.
  - `DateTime GetStartOfWeek(this DateTime date)`
  - 📁 DateTimeExtensions.cs

- **`GetUnixTimeFromDate`** → `long`
  - Clears the milliseconds.
  - `long GetUnixTimeFromDate(this DateTime theTime)`
  - 📁 DateTimeExtensions.cs

- **`GetUnixTimeFromDateSeconds`** → `long`
  - Clears the milliseconds.
  - `long GetUnixTimeFromDateSeconds(this DateTime theTime)`
  - 📁 DateTimeExtensions.cs

- **`GetWeekNumber`** → `int`
  - Gets current week number.
  - `int GetWeekNumber(this DateTime current)`
  - 📁 DateTimeExtensions.cs

- **`InRange`** → `bool`
  - Gets a value indicating if value is between or equal Minimum - Maximum values.
  - `bool InRange(this DateTime value, DateTime min, DateTime max)`
  - 📁 DateTimeExtensions.cs

- **`InSqlRange`** → `bool`
  - Gets a value indicating if value is between or equal Minimum - Maximum values for a SqlDateTime.
  - `bool InSqlRange(this DateTime value)`
  - 📁 DateTimeExtensions.cs

- **`IsAfter`** → `bool`
  - Gets a value indicating whether a day is after a specified date.
  - `bool IsAfter(this DateTime current, DateTime target)`
  - 📁 DateTimeExtensions.cs

- **`IsAfterOrEqual`** → `bool`
  - Gets a value indicating whether a day is after or equal a specified date.
  - `bool IsAfterOrEqual(this DateTime current, DateTime target)`
  - 📁 DateTimeExtensions.cs

- **`IsBeforeOrEqual`** → `bool`
  - Gets a value indicating whether a day is after or equal a specified date.
  - `bool IsBeforeOrEqual(this DateTime current, DateTime target)`
  - 📁 DateTimeExtensions.cs

- **`IsEarlierThan`** → `bool`
  - Clears the milliseconds.
  - `bool IsEarlierThan(this DateTime firstDateTime, DateTime secondDateTime)`
  - 📁 DateTimeExtensions.cs

- **`IsLaterThan`** → `bool`
  - Clears the milliseconds.
  - `bool IsLaterThan(this DateTime firstDateTime, DateTime secondDateTime)`
  - 📁 DateTimeExtensions.cs

- **`IsLeapYear`** → `bool`
  - Gets a value indicating whether the the provided date is in a leap year.
  - `bool IsLeapYear(this DateTime current)`
  - 📁 DateTimeExtensions.cs

- **`IsTheSameTimeAs`** → `bool`
  - Clears the milliseconds.
  - `bool IsTheSameTimeAs(this DateTime firstDateTime, DateTime secondDateTime)`
  - 📁 DateTimeExtensions.cs

- **`IsWeekDay`** → `bool`
  - Gets whether the the provided date is on a Week Day.
  - `bool IsWeekDay(this DateTime current)`
  - 📁 DateTimeExtensions.cs

- **`IsWeekend`** → `bool`
  - Gets whether the the provided date is on a Weekend.
  - `bool IsWeekend(this DateTime current)`
  - 📁 DateTimeExtensions.cs

- **`RoundUp`** → `DateTime`
  - Get a new date rounded to the specified time or a multiple of it
  - `DateTime RoundUp(this DateTime dateToRound, TimeSpan roundTo)`
  - 📁 DateTimeExtensions.cs

- **`ToA4DDateTimeString`** → `string`
  - Gets a formatted datestring from a date.
  - `string ToA4DDateTimeString(this DateTime current)`
  - 📁 DateTimeExtensions.cs

- **`ToA4DTimeString`** → `string`
  - Gets a formatted datestring from a date.
  - `string ToA4DTimeString(this DateTime current)`
  - 📁 DateTimeExtensions.cs

#### Extensions for `DateTime?`

- **`InSqlRangeOrNull`** → `bool`
  - Gets a value indicating if value is between or equal Minimum - Maximum values for a SqlDateTime or is null.
  - `bool InSqlRangeOrNull(this DateTime? value)`
  - 📁 DateTimeExtensions.cs

- **`IsAfter`** → `bool`
  - Gets a value indicating whether a day is after a specified date.
  - `bool IsAfter(this DateTime? current, DateTime? target)`
  - 📁 DateTimeExtensions.cs

#### Extensions for `DayOfWeek`

- **`GetDay`** → `double`
  - `double GetDay(this DayOfWeek weekday)`
  - 📁 DateTimeExtensions.cs

- **`GetLocalizedDayOfWeek`** → `string`
  - `string GetLocalizedDayOfWeek(this DayOfWeek dayOfWeek, CultureInfo culture)`
  - 📁 DateTimeExtensions.cs

#### Extensions for `DirectoryInfo`

- **`GetDescendantDirectory`** → `DirectoryInfo`
  - `DirectoryInfo GetDescendantDirectory(this DirectoryInfo dir, params string[] descendants)`
  - 📁 DirectoryInfoExtensions.cs

- **`GetDescendantFile`** → `FileInfo`
  - `FileInfo GetDescendantFile(this DirectoryInfo dir, params string[] descendants)`
  - 📁 DirectoryInfoExtensions.cs

- **`GetDescendantPath`** → `string`
  - `string GetDescendantPath(this DirectoryInfo dir, params string[] descendants)`
  - 📁 DirectoryInfoExtensions.cs

- **`IsNtfs`** → `bool`
  - `bool IsNtfs(this DirectoryInfo dir)`
  - 📁 DirectoryInfoExtensions.cs

#### Extensions for `double`

- **`GetTime`** → `string`
  - Gets the time.
  - `string GetTime(this double milliseconds)`
  - 📁 TimeSpanExtensions.cs

- **`PreciseEquals`** → `bool`
  - `bool PreciseEquals(this double left, double right, int floatDigits = 7)`
  - 📁 DoubleExtensions.cs

- **`RoundDown`** → `double`
  - Gets the time from .
  - `double RoundDown(this double i, double decimalPlaces)`
  - 📁 TimeSpanExtensions.cs

#### Extensions for `Enum`

- **`GetEnumValueDescription`** → `string`
  - Gets the enum value description.
  - `string GetEnumValueDescription(this Enum enumValue)`
  - 📁 EnumExtensions.cs

#### Extensions for `Environment.SpecialFolder`

- **`GetSpecialDirectory`** → `SpecialDirectory`
  - `SpecialDirectory GetSpecialDirectory(this Environment.SpecialFolder folder)`
  - 📁 DirectoryInfoExtensions.cs

- **`GetSpecialDirectoryPathDescendants`** → `string`
  - `string GetSpecialDirectoryPathDescendants(this Environment.SpecialFolder folder,
                                                            params string[] descendants)`
  - 📁 DirectoryInfoExtensions.cs

#### Extensions for `Exception`

- **`BuildMessage`** → `string`
  - Gets a formatted string from the exception.
  - `string BuildMessage(this Exception ex)`
  - 📁 ExceptionExtensions.cs

- **`SetStackTrace`** → `Exception`
  - Sets the stack trace of provided exception object
  - `Exception SetStackTrace(this Exception target, StackTrace stack)`
  - 📁 ExceptionExtensions.cs

#### Extensions for `ExpandoObject`

- **`AddPropertiesFromDictionary`** → `dynamic`
  - Adds the properties from dictionary.
  - `dynamic AddPropertiesFromDictionary(this ExpandoObject eo,
                                                      IDictionary<string, object> propsDictionary)`
  - 📁 DynamicExtensions.cs

- **`GetDynamicMemberNames`** → `IEnumerable<string>`
  - Gets the dynamic member names.
  - `IEnumerable<string> GetDynamicMemberNames(this ExpandoObject expandoObject)`
  - 📁 DynamicExtensions.cs

#### Extensions for `FileInfo`

- **`CompareSize`** → `int`
  - Compares the size.
  - `int CompareSize(this FileInfo file, long otherFileSize)`
  - 📁 FileInfoExtensions.cs

- **`GenerateMd5OfFile`** → `string`
  - `string GenerateMd5OfFile(this FileInfo file,
                                           bool removeDashes = true,
                                           bool toLower = true,
                                           bool asBase64String = false)`
  - 📁 FileInfoExtensions.cs

- **`IsNtfs`** → `bool`
  - `bool IsNtfs(this FileInfo file)`
  - 📁 FileInfoExtensions.cs

#### Extensions for `FileSystemInfo`

- **`GetDirectory`** → `DirectoryInfo`
  - `DirectoryInfo GetDirectory(this FileSystemInfo fileSystemInfo)`
  - 📁 FileSystemInfoExtensions.cs

- **`IsPathFile`** → `bool`
  - `bool IsPathFile(this FileSystemInfo fileSystemInfo)`
  - 📁 FileSystemInfoExtensions.cs

#### Extensions for `Guid`

- **`GetGuidString`** → `string`
  - `string GetGuidString(this Guid guid)`
  - 📁 StringExtensions.cs

#### Extensions for `Guid?`

- **`GetGuidString`** → `string`
  - `string GetGuidString(this Guid? guid)`
  - 📁 StringExtensions.cs

#### Extensions for `HttpResponseMessage`

- **`GetAllHeaders`** → `Dictionary<string, string[]>`
  - `Dictionary<string, string[]> GetAllHeaders(this HttpResponseMessage resp)`
  - 📁 WebResponseExtensions.cs

#### Extensions for `HttpWebRequest`

- **`PrepareRequest`** → `void`
  - `void PrepareRequest(this HttpWebRequest req, WebRequestParams pars)`
  - 📁 WebRequestExtensions.cs

#### Extensions for `HttpWebResponse`

- **`GetContentRange`** → `ContentRangeHeaderValue`
  - `ContentRangeHeaderValue GetContentRange(this HttpWebResponse response)`
  - 📁 WebResponseExtensions.cs

#### Extensions for `IAsyncHelper`

- **`FireOnMainThreadAndForget`** → `ITaskWrapper`
  - `ITaskWrapper FireOnMainThreadAndForget(this IAsyncHelper asyncHelper,
                                                         Action action,
                                                         CancellationToken cancellationToken = default)`
  - 📁 TaskExtensions.cs

- **`FireOnThreadPoolAndForget`** → `ITaskWrapper`
  - `ITaskWrapper FireOnThreadPoolAndForget(this IAsyncHelper asyncHelper,
                                                         Action action,
                                                         CancellationToken cancellationToken = default)`
  - 📁 TaskExtensions.cs

- **`FireTaskOnMainThreadAndForget`** → `ITaskWrapper`
  - `ITaskWrapper FireTaskOnMainThreadAndForget(this IAsyncHelper asyncHelper, Func<Task> task)`
  - 📁 TaskExtensions.cs

- **`FireTaskOnThreadPoolAndForget`** → `ITaskWrapper`
  - `ITaskWrapper FireTaskOnThreadPoolAndForget(this IAsyncHelper asyncHelper, Func<Task> task)`
  - 📁 TaskExtensions.cs

- **`FireTasksOnMainThreadAndForget`** → `IReadOnlyList<ITaskWrapper>`
  - `IReadOnlyList<ITaskWrapper> FireTasksOnMainThreadAndForget(this IAsyncHelper asyncHelper,
                                                                             IEnumerable<Func<Task>> tasks)`
  - 📁 TaskExtensions.cs

- **`FireTasksOnThreadPoolAndForget`** → `IReadOnlyList<ITaskWrapper>`
  - `IReadOnlyList<ITaskWrapper> FireTasksOnThreadPoolAndForget(this IAsyncHelper asyncHelper,
                                                                             IEnumerable<Func<Task>> tasks)`
  - 📁 TaskExtensions.cs

#### Extensions for `IEnumerable`

- **`Any`** → `bool`
  - `bool Any(this IEnumerable source)`
  - 📁 EnumerableExtensions.cs

- **`Count`** → `int`
  - `int Count(this IEnumerable source)`
  - 📁 CollectionExtensions.cs

- **`GetItemType`** → `Type`
  - Gets the type of the item.
  - `Type GetItemType(this IEnumerable enumerable)`
  - 📁 EnumerableExtensions.cs

- **`HasAny`** → `bool`
  - `bool HasAny(this IEnumerable enumerable)`
  - 📁 EnumerableExtensions.cs

- **`UnorderedSequenceEqual`** → `bool`
  - Checks if two sequences contain the same elements without checking their order
  - `bool UnorderedSequenceEqual(this IEnumerable first, IEnumerable second)`
  - 📁 EnumerableExtensions.cs

#### Extensions for `IEnumerable<string>`

- **`AggregateSafe`** → `string`
  - Aggregates a list of strings.
  - `string AggregateSafe(this IEnumerable<string> source)`
  - 📁 EnumerableExtensions.cs

#### Extensions for `IError`

- **`GetErrorRoot`** → `IError`
  - `IError GetErrorRoot(this IError error)`
  - 📁 ErrorExtensions.cs

- **`ToAggregatedError`** → `AggregatedError`
  - `AggregatedError ToAggregatedError(this IError error)`
  - 📁 ErrorExtensions.cs

#### Extensions for `int`

- **`Add`** → `int`
  - Adds the value to source.
  - `int Add(this int source, int value)`
  - 📁 IntegerExtensions.cs

- **`Days`** → `TimeSpan`
  - Gets a TimeSpan for n number of Days.
  - `TimeSpan Days(this int number)`
  - 📁 TimeSpanExtensions.cs

- **`Hours`** → `TimeSpan`
  - Gets a TimeSpan for n number of Hours.
  - `TimeSpan Hours(this int number)`
  - 📁 TimeSpanExtensions.cs

- **`InRange`** → `bool`
  - Gets a value indicating if value is between or equal Minimum - Maximum values.
  - `bool InRange(this int value, int min, int max)`
  - 📁 IntegerExtensions.cs

- **`InRange`** → `bool`
  - Gets a value indicating if value is between (or equal) Minimum - Maximum values.
  - `bool InRange(this int value, int min, int max, bool includeMinMaxValues)`
  - 📁 IntegerExtensions.cs

- **`IsLessThanZero`** → `bool`
  - Indicates that specified value is less than zero.
  - `bool IsLessThanZero(this int value)`
  - 📁 IntegerExtensions.cs

- **`IsNotZero`** → `bool`
  - Indicates that specified value is > than 0
  - `bool IsNotZero(this int value)`
  - 📁 IntegerExtensions.cs

- **`IsZero`** → `bool`
  - Indicates that specified value is equal to 0
  - `bool IsZero(this int value)`
  - 📁 IntegerExtensions.cs

- **`Minutes`** → `TimeSpan`
  - Gets a TimeSpan for n number of Minutes.
  - `TimeSpan Minutes(this int number)`
  - 📁 TimeSpanExtensions.cs

- **`Seconds`** → `TimeSpan`
  - Gets a TimeSpan for n number of Seconds.
  - `TimeSpan Seconds(this int number)`
  - 📁 TimeSpanExtensions.cs

- **`ToDaySuffix`** → `string`
  - Gets a value indicating whether a day is after or equal a specified date.
  - `string ToDaySuffix(this int day)`
  - 📁 DateTimeExtensions.cs

- **`ToShort`** → `short`
  - Converts the integer to a byte.
  - `short ToShort(this int value)`
  - 📁 IntegerExtensions.cs

#### Extensions for `int?`

- **`ToByteSafe`** → `byte`
  - Converts the integer to a byte.
  - `byte ToByteSafe(this int? value)`
  - 📁 IntegerExtensions.cs

- **`ToByteSafe`** → `byte`
  - Converts the integer to a byte.
  - `byte ToByteSafe(this int? value, byte defaultValue)`
  - 📁 IntegerExtensions.cs

- **`ToStringSafe`** → `string`
  - Converts the integer to a string.
  - `string ToStringSafe(this int? value)`
  - 📁 IntegerExtensions.cs

- **`ToStringSafe`** → `string`
  - Converts the integer to a string.
  - `string ToStringSafe(this int? value, string defaultValue)`
  - 📁 IntegerExtensions.cs

#### Extensions for `long`

- **`DateTimeFromUnixTimestampMillis`** → `DateTime`
  - Clears the milliseconds.
  - `DateTime DateTimeFromUnixTimestampMillis(this long millis)`
  - 📁 DateTimeExtensions.cs

- **`DateTimeFromUnixTimestampSeconds`** → `DateTime`
  - Clears the milliseconds.
  - `DateTime DateTimeFromUnixTimestampSeconds(this long seconds)`
  - 📁 DateTimeExtensions.cs

- **`FromTicks`** → `DateTime`
  - `DateTime FromTicks(this long ticks)`
  - 📁 DateTimeExtensions.cs

- **`GetTime`** → `string`
  - Gets the time.
  - `string GetTime(this long milliseconds)`
  - 📁 TimeSpanExtensions.cs

#### Extensions for `NotifyCollectionChangedEventHandler`

- **`HandleCollectionChanged`** → `void`
  - `void HandleCollectionChanged(this NotifyCollectionChangedEventHandler collectionChangedEventHandler,
                                               object sender,
                                               NotifyCollectionChangedEventArgs eventArgs)`
  - 📁 EventsExtensions.cs

#### Extensions for `object`

- **`GetTypeInstanceDescription`** → `string`
  - Gets the value of objects property by its name.
  - `string GetTypeInstanceDescription(this object value)`
  - 📁 ObjectExtensions.cs

- **`IsNullOrEmpty`** → `bool`
  - `bool IsNullOrEmpty(this object data)`
  - 📁 ObjectExtensions.cs

#### Extensions for `PropertyChangedEventHandler`

- **`HandlePropertyChanged`** → `void`
  - `void HandlePropertyChanged(this PropertyChangedEventHandler handler,
                                             object sender,
                                             string propertyName)`
  - 📁 EventsExtensions.cs

- **`HandlePropertyChanged`** → `void`
  - `void HandlePropertyChanged(this PropertyChangedEventHandler propertyChangedEventHandler,
                                             object sender,
                                             PropertyChangedEventArgs eventArgs)`
  - 📁 EventsExtensions.cs

#### Extensions for `SemaphoreSlim`

- **`SafeRelease`** → `int`
  - Exits the a specified number of times.
  - `int SafeRelease(this SemaphoreSlim semaphore, int releaseCount = 1)`
  - 📁 SemaphoreSlimExtensions.cs

#### Extensions for `Stopwatch`

- **`GetTime`** → `string`
  - Gets the time from .
  - `string GetTime(this Stopwatch stopwatch)`
  - 📁 TimeSpanExtensions.cs

#### Extensions for `Stream`

- **`ComputeMd5Hash`** → `string`
  - `string ComputeMd5Hash(this Stream data,
                                        bool removeDashes = true,
                                        bool toLower = true,
                                        bool asBase64String = false)`
  - 📁 StreamExtensions.cs

- **`CopyStreamToStream`** → `void`
  - `void CopyStreamToStream(this Stream sourceStream,
                                          Stream destStream,
                                          Action<double> progressMaximumSet = null,
                                          Action<double> progressValueSet = null,
                                          long? length = null)`
  - 📁 StreamExtensions.cs

- **`CopyTo`** → `void`
  - `void CopyTo(this Stream src, Stream dest)`
  - 📁 StringExtensions.cs

- **`CopyToMemoryStream`** → `MemoryStream`
  - `MemoryStream CopyToMemoryStream(this Stream streamToCopy, bool disposeSource = false)`
  - 📁 StreamExtensions.cs

#### Extensions for `string`

- **`CompareOrdinalIgnoreCase`** → `bool`
  - Converts a string to proper case, handling special cases for names, Scottish prefixes, and Roman numerals.
  - `bool CompareOrdinalIgnoreCase(this string source, string value)`
  - 📁 StringExtensions.cs

- **`ComputeMd5Hash`** → `string`
  - Generate MD5 hash from the specified string
  - `string ComputeMd5Hash(this string value)`
  - 📁 StringExtensions.cs

- **`ComputeSha256Hash`** → `string`
  - `string ComputeSha256Hash(this string rawData)`
  - 📁 StringExtensions.cs

- **`Contains`** → `bool`
  - `bool Contains(this string source, string toCheck, StringComparison comp)`
  - 📁 StringExtensions.cs

- **`ContainsAny`** → `bool`
  - Returns a value indicating whether any of a set of specified substrings occurs within this string.
  - `bool ContainsAny(this string value, IEnumerable<string> toCheck, StringComparison comparisonType)`
  - 📁 StringExtensions.cs

- **`ContainsOnlyLetters`** → `bool`
  - Determines whether the specified string is not null or white space.
  - `bool ContainsOnlyLetters(this string text)`
  - 📁 StringExtensions.cs

- **`ContainsOnlyLettersAndNumbers`** → `bool`
  - Determines whether the specified string is not null or white space.
  - `bool ContainsOnlyLettersAndNumbers(this string text)`
  - 📁 StringExtensions.cs

- **`ContainsOnlyLettersNumbersAndUnderscore`** → `bool`
  - Determines whether the specified string is not null or white space.
  - `bool ContainsOnlyLettersNumbersAndUnderscore(this string text)`
  - 📁 StringExtensions.cs

- **`ContainsOrdinalIgnoreCase`** → `bool`
  - Indicates whether a string contains another string under comparison.
  - `bool ContainsOrdinalIgnoreCase(this string str, string other)`
  - 📁 StringExtensions.cs

- **`DealWithRomanNumerals`** → `string`
  - `string DealWithRomanNumerals(this string word)`
  - 📁 StringUtilities.cs

- **`DivideByCapital`** → `string`
  - Divides the by capital letter.
  - `string DivideByCapital(this string value)`
  - 📁 StringExtensions.cs

- **`EscapeSqlWildCards`** → `string`
  - Escape SQL wild cards characters.
  - `string EscapeSqlWildCards(this string value)`
  - 📁 StringExtensions.cs

- **`FirstCharToLower`** → `string`
  - `string FirstCharToLower(this string input)`
  - 📁 StringExtensions.cs

- **`FirstCharToUpper`** → `string`
  - `string FirstCharToUpper(this string input)`
  - 📁 StringExtensions.cs

- **`FromBase64`** → `string`
  - `string FromBase64(this string base64EncodedData, Encoding enc = null)`
  - 📁 StringExtensions.cs

- **`FromString`** → `decimal`
  - Converts string to decimal value and replace symbols with CurrentCulture NumberDecimalSeparator
  - `decimal FromString(this string value)`
  - 📁 StringExtensions.cs

- **`GenerateMd5OfString`** → `string`
  - Generates the md5 of string.
  - `string GenerateMd5OfString(this string value)`
  - 📁 StringExtensions.cs

- **`GetContentRange`** → `ContentRangeHeaderValue`
  - `ContentRangeHeaderValue GetContentRange(this string rangeHeader)`
  - 📁 WebResponseExtensions.cs

- **`GetPathParts`** → `IEnumerable<string>`
  - `IEnumerable<string> GetPathParts(this string path)`
  - 📁 StringExtensions.cs

- **`HasNoSqlWildCards`** → `bool`
  - Determines whether given string has no SQL wild cards.
  - `bool HasNoSqlWildCards(this string value)`
  - 📁 StringExtensions.cs

- **`HasNoWildCards`** → `bool`
  - Determines whether given string has no wild cards.
  - `bool HasNoWildCards(this string value)`
  - 📁 StringExtensions.cs

- **`IndexOf`** → `int`
  - Searches for the index of the first occurrence of the specified strings in the input string.
  - `int IndexOf(this string value, params string[] matchCandidates)`
  - 📁 StringExtensions.cs

- **`IsAllUpperOrAllLower`** → `bool`
  - Converts a string to proper case, handling special cases for names, Scottish prefixes, and Roman numerals.
  - `bool IsAllUpperOrAllLower(this string input)`
  - 📁 StringExtensions.cs

- **`IsAWord`** → `bool`
  - Determines whether the specified string is not null or white space.
  - `bool IsAWord(this string text)`
  - 📁 StringExtensions.cs

- **`IsBothNullOrEqual`** → `bool`
  - `bool IsBothNullOrEqual(this string source,
                                         string value,
                                         StringComparison comparisonType = StringComparison.Ordinal)`
  - 📁 StringExtensions.cs

- **`IsEqual`** → `bool`
  - Compare 2 strings, ignoring case.
  - `bool IsEqual(this string source,
                               string value,
                               StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)`
  - 📁 StringExtensions.cs

- **`IsNotEqual`** → `bool`
  - Determines whether string is not equal to the specified value.
  - `bool IsNotEqual(this string source,
                                  string value,
                                  StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)`
  - 📁 StringExtensions.cs

- **`IsNotNullOrEmptyOrWhiteSpace`** → `bool`
  - Determines whether the specified string is not null or white space.
  - `bool IsNotNullOrEmptyOrWhiteSpace(this string value)`
  - 📁 StringExtensions.cs

- **`IsNotNullOrEmptyString`** → `bool`
  - Gets a value indicating if the string is NOT Null or Empty.
  - `bool IsNotNullOrEmptyString(this string value)`
  - 📁 StringExtensions.cs

- **`IsNotNullOrWhiteSpace`** → `bool`
  - Determines whether the specified string is not null or white space.
  - `bool IsNotNullOrWhiteSpace(this string value)`
  - 📁 StringExtensions.cs

- **`IsNullOrEmptyOrWhiteSpace`** → `bool`
  - Determines whether the specified string is not null or white space.
  - `bool IsNullOrEmptyOrWhiteSpace(this string value)`
  - 📁 StringExtensions.cs

- **`IsNullOrEmptyString`** → `bool`
  - Gets a value indicating if the string is Null or Empty.
  - `bool IsNullOrEmptyString(this string value)`
  - 📁 StringExtensions.cs

- **`IsNullOrWhiteSpace`** → `bool`
  - Determines whether the specified string is null or white space.
  - `bool IsNullOrWhiteSpace(this string value)`
  - 📁 StringExtensions.cs

- **`Left`** → `string`
  - Returns a string containing a specified number of characters from the left side of a string.
  - `string Left(this string str, int length, bool trim = false)`
  - 📁 StringExtensions.cs

- **`Length`** → `int`
  - Returns a string containing a specified number of characters from the right side of a string.
  - `int Length(this string value, bool trim = true)`
  - 📁 StringExtensions.cs

- **`MatchesRegex`** → `bool`
  - Determines whether the specified string is not null or white space.
  - `bool MatchesRegex(this string text, string regexPattern)`
  - 📁 StringExtensions.cs

- **`MatchesRegex`** → `bool`
  - Determines whether the specified string is not null or white space.
  - `bool MatchesRegex(this string text, Regex regex)`
  - 📁 StringExtensions.cs

- **`Mid`** → `string`
  - Returns a string that contains all the characters starting from a specified position in a string.
  - `string Mid(this string str, int start, bool trim = false)`
  - 📁 StringExtensions.cs

- **`Mid`** → `string`
  - Returns a string that contains a specified number of characters starting from a specified position in a string.
  - `string Mid(this string str, int start, int length, bool trim = false)`
  - 📁 StringExtensions.cs

- **`NormalizeLineBreaks`** → `string`
  - `string NormalizeLineBreaks(this string input)`
  - 📁 StringExtensions.cs

- **`Remove`** → `string`
  - Removes the specified strings from current string.
  - `string Remove(this string text, params string[] strings)`
  - 📁 StringExtensions.cs

- **`Remove`** → `string`
  - Removes the specified chars from current string.
  - `string Remove(this string text, params char[] chars)`
  - 📁 StringExtensions.cs

- **`RemoveDiacritics`** → `string`
  - Generates the md5 of string.
  - `string RemoveDiacritics(this string text)`
  - 📁 StringExtensions.cs

- **`ReplaceStandardWildCardsBySql`** → `string`
  - Replaces the standard wild cards by SQL ones.
  - `string ReplaceStandardWildCardsBySql(this string value)`
  - 📁 StringExtensions.cs

- **`Right`** → `string`
  - Returns a string containing a specified number of characters from the right side of a string.
  - `string Right(this string str, int length, bool trim = false)`
  - 📁 StringExtensions.cs

- **`Split`** → `IList<string>`
  - Splits the specified string into parts.
  - `IList<string> Split(this string value, string splitValue, int stringLength)`
  - 📁 StringExtensions.cs

- **`StringToByteArray`** → `byte[]`
  - `byte[] StringToByteArray(this string hex)`
  - 📁 StringExtensions.cs

- **`ToBase64`** → `string`
  - Returns a string containing a specified number of characters from the right side of a string.
  - `string ToBase64(this string str, Encoding enc = null)`
  - 📁 StringExtensions.cs

- **`ToCamel`** → `string`
  - The SQL wild card 'any value'.
  - `string ToCamel(this string text)`
  - 📁 StringExtensions.cs

- **`ToDebug`** → `string`
  - Writes value to the debug output.
  - `string ToDebug(this string value)`
  - 📁 StringExtensions.cs

- **`ToDouble`** → `double`
  - `double ToDouble(this string value)`
  - 📁 DoubleExtensions.cs

- **`ToFormattedPhoneNumber`** → `string`
  - The SQL wild card 'any value'.
  - `string ToFormattedPhoneNumber(this string phoneNumber)`
  - 📁 StringExtensions.cs

- **`ToInt`** → `int`
  - Converts string to int, returns -1 if conversion fails
  - `int ToInt(this string value)`
  - 📁 StringExtensions.cs

- **`ToNiceString`** → `string`
  - The SQL wild card 'any value'.
  - `string ToNiceString(this string text)`
  - 📁 StringExtensions.cs

- **`ToProperCase`** → `string`
  - Converts a string to proper case, handling special cases for names, Scottish prefixes, and Roman numerals.
  - `string ToProperCase(this string input)`
  - 📁 StringExtensions.cs

- **`ToStream`** → `Stream`
  - Converts the specified string to stream.
  - `Stream ToStream(this string str)`
  - 📁 StringExtensions.cs

- **`ToTrace`** → `string`
  - Writes value to the trace output.
  - `string ToTrace(this string value)`
  - 📁 StringExtensions.cs

- **`ToUri`** → `Uri`
  - Converts string to decimal value and replace symbols with CurrentCulture NumberDecimalSeparator
  - `Uri ToUri(this string source, Uri baseUri = null, UriKind kind = UriKind.Absolute)`
  - 📁 StringExtensions.cs

- **`TrimLength`** → `string`
  - `string TrimLength(this string value, int length, bool trim = false)`
  - 📁 StringExtensions.cs

- **`Unzip`** → `string`
  - `string Unzip(this string bytes)`
  - 📁 StringExtensions.cs

- **`ValidateCheckDigit`** → `bool`
  - Modulus 10 algorithm created by Hans Peter Luhn. Uses a weight of 2 which is applied to every odd position digit.
  - `bool ValidateCheckDigit(this string value)`
  - 📁 StringExtensions.cs

- **`Zip`** → `string`
  - `string Zip(this string str)`
  - 📁 StringExtensions.cs

#### Extensions for `StringBuilder`

- **`AppendJoin`** → `void`
  - `void AppendJoin(this StringBuilder stringBuilder, IEnumerable collection)`
  - 📁 StringExtensions.cs

#### Extensions for `Task`

- **`IsFailed`** → `bool`
  - Waits for task to start.
  - `bool IsFailed(this Task task)`
  - 📁 TaskExtensions.cs

- **`IsFinished`** → `bool`
  - Waits for task to start.
  - `bool IsFinished(this Task task)`
  - 📁 TaskExtensions.cs

- **`IsNotStarted`** → `bool`
  - Waits for task to start.
  - `bool IsNotStarted(this Task task)`
  - 📁 TaskExtensions.cs

- **`IsRunning`** → `bool`
  - Waits for task to start.
  - `bool IsRunning(this Task task)`
  - 📁 TaskExtensions.cs

#### Extensions for `TimeSpan`

- **`GetTime`** → `string`
  - Gets the time from .
  - `string GetTime(this TimeSpan timespan, int decimals = 0)`
  - 📁 TimeSpanExtensions.cs

- **`IsAm`** → `bool`
  - `bool IsAm(this TimeSpan timeSpan)`
  - 📁 TimeSpanExtensions.cs

- **`IsMidnight`** → `bool`
  - Gets a value indicating if the time is midnight (00:00:00).
  - `bool IsMidnight(this TimeSpan value)`
  - 📁 TimeSpanExtensions.cs

- **`RoundUp`** → `TimeSpan`
  - Gets the time from .
  - `TimeSpan RoundUp(this TimeSpan timeSpan, int roundToMinutes)`
  - 📁 TimeSpanExtensions.cs

#### Extensions for `Type`

- **`GetBaseTypes`** → `List<Type>`
  - Gets the base types.
  - `List<Type> GetBaseTypes(this Type baseType, List<Type> baseTypes = null)`
  - 📁 TypeExtensions.cs

- **`GetImplementedClasses`** → `IEnumerable<Type>`
  - Gets the implemented classes.
  - `IEnumerable<Type> GetImplementedClasses(this Type baseType)`
  - 📁 TypeExtensions.cs

- **`GetImplementedInterfaces`** → `IEnumerable<Type>`
  - Gets the implemented interfaces.
  - `IEnumerable<Type> GetImplementedInterfaces(this Type interfaceType)`
  - 📁 TypeExtensions.cs

- **`GetTypeDescription`** → `string`
  - Gets the base types.
  - `string GetTypeDescription(this Type value)`
  - 📁 TypeExtensions.cs

- **`IsOrInherits`** → `bool`
  - `bool IsOrInherits(this Type type, Type typeToCompare)`
  - 📁 TypeExtensions.cs

#### Extensions for `Uri`

- **`GetHttpRequest`** → `HttpWebRequest`
  - `HttpWebRequest GetHttpRequest(this Uri url, WebRequestParams pars = null)`
  - 📁 UriExtensions.cs

- **`GetWebRequest`** → `WebRequest`
  - `WebRequest GetWebRequest(this Uri url, WebRequestParams pars = null)`
  - 📁 UriExtensions.cs

#### Extensions for `Version`

- **`FirstIsHigherThanSecond`** → `bool`
  - `bool FirstIsHigherThanSecond(this Version first, Version second)`
  - 📁 VersionExtensions.cs

- **`FirstIsOtherThanSecond`** → `bool`
  - `bool FirstIsOtherThanSecond(this Version first, Version second)`
  - 📁 VersionExtensions.cs

#### Extensions for `WebClient`

- **`PrepareWebClient`** → `void`
  - `void PrepareWebClient(this WebClient client, WebRequestParams pars)`
  - 📁 WebClientExtensions.cs

#### Extensions for `WebRequest`

- **`PrepareRequest`** → `void`
  - `void PrepareRequest(this WebRequest req, WebRequestParams pars)`
  - 📁 WebRequestExtensions.cs

#### Extensions for `WebRequestParams`

- **`GetHttpClientHandler`** → `HttpClientHandler`
  - `HttpClientHandler GetHttpClientHandler(this WebRequestParams pars)`
  - 📁 WebRequestParamsExtensions.cs

- **`Merge`** → `WebRequestParams`
  - `WebRequestParams Merge(this WebRequestParams pars, WebRequestParams other)`
  - 📁 WebRequestParamsExtensions.cs

#### Extensions for `WebResponse`

- **`GetAllHeaders`** → `Dictionary<string, string>`
  - `Dictionary<string, string> GetAllHeaders(this WebResponse resp)`
  - 📁 WebResponseExtensions.cs

#### Extensions for `XContainer`

- **`CreateAttribute`** → `XContainer`
  - Creates a attribute.
  - `XContainer CreateAttribute(this XContainer element, string attributeName)`
  - 📁 XElementExtensions.cs

- **`CreateAttribute`** → `XContainer`
  - Creates a attribute.
  - `XContainer CreateAttribute(this XContainer element, string attributeName, string value)`
  - 📁 XElementExtensions.cs

- **`CreateChild`** → `XElement`
  - Creates a child element.
  - `XElement CreateChild(this XContainer element, string childName)`
  - 📁 XElementExtensions.cs

- **`CreateChild`** → `XElement`
  - Creates a child element.
  - `XElement CreateChild(this XContainer element, string childName, string value)`
  - 📁 XElementExtensions.cs

#### Extensions for `XElement`

- **`GetAttributeValue`** → `string`
  - Gets a attribute value.
  - `string GetAttributeValue(this XElement element, string attributeName)`
  - 📁 XElementExtensions.cs

- **`GetChild`** → `XElement`
  - Gets the child.
  - `XElement GetChild(this XElement element, string childName)`
  - 📁 XElementExtensions.cs

- **`GetChild`** → `XElement`
  - Gets the child.
  - `XElement GetChild(this XElement element, string childName, string nameSpace)`
  - 📁 XElementExtensions.cs

- **`GetChildValue`** → `string`
  - Gets a value for a child.
  - `string GetChildValue(this XElement element, string childName)`
  - 📁 XElementExtensions.cs

- **`GetChildValue`** → `string`
  - Gets the child value.
  - `string GetChildValue(this XElement element, string childName, string nameSpace)`
  - 📁 XElementExtensions.cs

- **`GetOrCreateAttribute`** → `XAttribute`
  - Gets a attribute (if the attribute not exist it's created).
  - `XAttribute GetOrCreateAttribute(this XElement element, string attributeName)`
  - 📁 XElementExtensions.cs

- **`GetOrCreateChild`** → `XElement`
  - Gets a element (if the element not exist it's created).
  - `XElement GetOrCreateChild(this XElement element, string childName)`
  - 📁 XElementExtensions.cs

- **`GetOrCreateChild`** → `XElement`
  - Gets a element (if the element not exist it's created).
  - `XElement GetOrCreateChild(this XElement element, string childName, string value)`
  - 📁 XElementExtensions.cs

- **`SetChildValue`** → `XElement`
  - Sets the value for an element.
  - `XElement SetChildValue(this XElement element, string value)`
  - 📁 XElementExtensions.cs

#### Extensions for `XmlSchemaSet`

- **`Add`** → `XmlSchema`
  - `XmlSchema Add(this XmlSchemaSet xmlSchemaSet, string targetNamespace, Stream schemaStream)`
  - 📁 XmlExtensions.cs

- **`AddManifestResourceSchema`** → `XmlSchema`
  - `XmlSchema AddManifestResourceSchema(this XmlSchemaSet xmlSchemaSet,
                                                      Assembly resourceAssembly,
                                                      string targetNamespace,
                                                      string name)`
  - 📁 XmlExtensions.cs

#### Extensions for `XNode`

- **`ToXmlElement`** → `IXPathNavigable`
  - Convert to a XML element.
  - `IXPathNavigable ToXmlElement(this XNode el)`
  - 📁 XElementExtensions.cs

#### Extensions for `XObject`

- **`Create`** → `XAttribute`
  - Creates a attribute.
  - `XAttribute Create(this XObject attribute, string attributeName)`
  - 📁 XElementExtensions.cs

- **`Create`** → `XAttribute`
  - Creates a attribute.
  - `XAttribute Create(this XObject attribute, string attributeName, string value)`
  - 📁 XElementExtensions.cs

### 🔷 Interfaces

- **`IAppVersionProvider`**
- **`IAsyncHelper`**
- **`IConcurrentList`**
- **`IError`**
- **`IExceptionError`**
- **`IExceptionHandlerOptions`**
- **`IFExInitializable`**
- **`IFExInitialize`**
- **`IFExLogger`**
- **`IFExMemoryCache`**
- **`IFExNotifyPropertyChanged`**
- **`IFExPriorityInitialize`**
- **`IIndex`**
- **`IMap`**
- **`IResult`**
- **`IResult`**
- **`IResult`**
- **`IStackError`**
- **`ISuppressEvents`**
- **`ISynchronizedAccessService`**
- **`ITaskWrapper`**
- **`ITaskWrapper`**
- **`ITaskWrapperBase`**
- **`ITaskWrapperBase`**

### 📦 Classes

- **`AggregatedError`**
- **`ArrayExtensions`**
- **`AsyncOptionsExtensions`**
- **`AsyncStatics`**
- **`AttachedException`**
- **`BaseConcurrentList`**
- **`BooleanExtensions`** *(static)*
- **`CollectionEventsConfig`**
- **`CollectionExtensions`**
- **`DateTimeDefaults`** - Provides set of default datetime values.
- **`DateTimeExtensions`**
- **`DictionaryExtensions`** - IDictionary extensions class.
- **`DirectoryInfoExtensions`**
- **`DisposableAction`** - Models a disposable action that is guaranteed to be invoked at least on disposal (if not explicitly invoked).
- **`DoubleExtensions`**
- **`DynamicExtensions`** - Extension methods for the Dynamic
- **`EnumerableExtensions`**
- **`EnumerableExtensions`**
- **`EnumExtensions`**
- **`EqualityHelper`**
- **`Error`**
- **`Error`**
- **`ErrorExtensions`**
- **`EventArgsCache`**
- **`EventsExtensions`**
- **`ExceptionError`**
- **`ExceptionExtensions`**
- **`FExAgnosticsStatics`** - Provides static access to core FEx services at the agnostics layer. This class MUST be initialized by higher-level layers (e.g., FEx.Core) before use.
- **`FExConversion`** *(static)*
- **`FExDebugLogger`**
- **`FExErrorEventArgs`**
- **`FExInitialize`**
- **`FExSemaphoreSlim`**
- **`FExStaticLogger`**
- **`FileInfoExtensions`**
- **`FileLengthConverter`**
- **`FileSystemHelper`**
- **`FileSystemInfoExtensions`** *(static)*
- **`GuardExtensions`**
- **`HashHelper`** *(static)*
- **`Index`**
- **`InitializationExtensions`**
- **`IntegerExtensions`** *(static)*
- **`InterlockedBool`**
- **`LambdaEqualityHelper`**
- **`LambdaExtensions`**
- **`LambdaExtensions`** - Returns a expression that always returns false
- **`ListExtensions`** - Extensions for the IList interface.
- **`Map`**
- **`ObjectExtensions`**
- **`ReadOnlyDictionaryExtensions`** - IReadOnlyDictionary extensions class.
- **`Result`**
- **`Result`**
- **`ResultBase`**
- **`SemaphoreSlimExtensions`**
- **`SpecialDirectory`**
- **`StreamExtensions`**
- **`StringExtensions`** - String extensions class - comprehensive utilities for string manipulation.
- **`StringUtilities`** - String utility methods for advanced string processing.
- **`SuppressEventsDisposable`**
- **`TaskExtensions`**
- **`TimeSpanExtensions`** - Extension methods for the TimeSpan
- **`TypeExtensions`**
- **`UriExtensions`**
- **`VBConversion`** *(static)*
- **`VersionExtensions`** *(static)*
- **`WebClientExtensions`**
- **`WebRequestExtensions`**
- **`WebRequestParams`**
- **`WebRequestParamsExtensions`**
- **`WebResponseExtensions`**
- **`WhenResult`**
- **`WhenResultExtensions`**
- **`XElementExtensions`**
- **`XmlExtensions`**

### 🔢 Enums

- **`AsyncMode`**
- **`AsyncOptions`**
- **`DateInterval`** - Indicates how to determine and format date intervals when calling date-related functions.
- **`FileOperation`**
- **`LengthType`**
- **`MediaTypes`**
- **`WildCardPosition`** - Wild card position.

### ⚙️ Public Methods

#### 📁 AsyncStatics.cs (9 methods)

- **`DelayAsync`** → `Task`
  - Creates a cancellable task that completes after a time delay.
  - `Task DelayAsync(int millisecondsDelay, CancellationToken cancellationToken = default)`

- **`DelayAsync`** → `Task`
  - Creates a cancellable task that completes after a specified time interval.
  - `Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)`

- **`DelayUntilAsync`** → `Task`
  - Waits asynchronously the specified amount of milliseconds.
  - `Task DelayUntilAsync(Func<bool> predicate,
                                             Action action = null,
                                             double milliseconds = 0,
                                             CancellationToken cancellationToken = default)`

- **`DelayUntilAsync`** → `Task`
  - Waits asynchronously the specified amount of milliseconds.
  - `Task DelayUntilAsync(Func<Task<bool>> predicate,
                                             Action action = null,
                                             double milliseconds = 0,
                                             CancellationToken cancellationToken = default)`

- **`DelayWithTimespanUntilAsync`** → `Task`
  - Waits asynchronously the specified amount of milliseconds.
  - `Task DelayWithTimespanUntilAsync(Func<bool> predicate,
                                                         Action action = null,
                                                         TimeSpan? timeSpan = null,
                                                         CancellationToken cancellationToken = default)`

- **`ExecuteOnThreadPoolAsync`** → `Task`
  - `Task ExecuteOnThreadPoolAsync(Action action,
                                                      AsyncOptions options = AsyncOptions.ImmediateStart,
                                                      CancellationToken cancellationToken = default)`

- **`ExecuteTaskOnThreadPoolAsync`** → `Task`
  - `Task ExecuteTaskOnThreadPoolAsync(Func<Task> func,
                                                          AsyncOptions options = AsyncOptions.ImmediateStart)`

- **`WaitAndInvokeActionAsync`** → `Task`
  - Waits asynchronously the specified amount of milliseconds and invokes action.
  - `Task WaitAndInvokeActionAsync(double delayMilliseconds = 0,
                                                      Action action = null,
                                                      CancellationToken cancellationToken = default)`

- **`WaitAndInvokeActionAsync`** → `Task`
  - Waits asynchronously the specified amount of milliseconds and invokes action.
  - `Task WaitAndInvokeActionAsync(TimeSpan? delayTimeSpan = null,
                                                      Action action = null,
                                                      CancellationToken cancellationToken = default)`

#### 📁 DateTimeExtensions.cs

- **`GetCurrentUnixTimestampMillis`** → `long`
  - Clears the milliseconds.
  - `long GetCurrentUnixTimestampMillis()`

- **`GetCurrentUnixTimestampSeconds`** → `long`
  - Clears the milliseconds.
  - `long GetCurrentUnixTimestampSeconds()`

#### 📁 DisposableAction.cs

- **`Dispose`** → `void`
  - Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
  - `void Dispose()`

#### 📁 DynamicExtensions.cs

- **`DynamicObjectToDictionary`** → `IDictionary<string, object>`
  - Adds the properties from dictionary.
  - `IDictionary<string, object> DynamicObjectToDictionary(dynamic src)`

#### 📁 Error.cs

- **`SetInnerError`** → `void`
  - `void SetInnerError(IError innerError)`

#### 📁 FExAgnosticsStatics.cs

- **`Configure`** → `void`
  - Configures the static services. This should be called during application initialization.
  - `void Configure(IAsyncHelper asyncHelper)`

#### 📁 FExConversion.cs

- **`Fix`** → `double`
  - Return the integer portion of a number.
  - `double Fix(double number)`

#### 📁 FileLengthConverter.cs (7 methods)

- **`ConvertFileLength`** → `double`
  - Converts the length of the file.
  - `double ConvertFileLength(long size, LengthType input, LengthType output, int digits = 3)`

- **`ConvertFileLength`** → `double`
  - Converts the length of the file.
  - `double ConvertFileLength(FileInfo fi, LengthType output, int digits = 3)`

- **`ConvertFileLengthToString`** → `string`
  - Converts the length of the file.
  - `string ConvertFileLengthToString(double size, LengthType input, LengthType output, int digits = 3)`

- **`GetLength`** → `double`
  - `double GetLength(LengthType lengthType)`

- **`GetLengthType`** → `LengthType`
  - `LengthType GetLengthType(string unitShortcut)`

- **`GetOutputLenghtType`** → `LengthType`
  - Converts the length of the file.
  - `LengthType GetOutputLenghtType(long size)`

- **`GetOutputLenghtType`** → `LengthType`
  - Converts the length of the file.
  - `LengthType GetOutputLenghtType(double size)`

#### 📁 FileSystemHelper.cs

- **`FixPath`** → `string`
  - Generates the md5 of file.
  - `string FixPath(string path)`

- **`GenerateMd5OfFile`** → `string`
  - Generates the md5 of file.
  - `string GenerateMd5OfFile(string filePath)`

- **`GetParentFolderFromPath`** → `string`
  - Generates the md5 of file.
  - `string GetParentFolderFromPath(string path, char pathSeparator, bool includeSeparatorAtEnd)`

- **`IsPathNtfs`** → `bool`
  - Determines whether [is path NTFS] [the specified absolute file path].
  - `bool IsPathNtfs(string absolutePath)`

#### 📁 InterlockedBool.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

- **`TrySet`** → `bool`
  - This method sets a value
  - `bool TrySet(bool value)`

#### 📁 LambdaEqualityHelper.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode(T instance)`

#### 📁 Map.cs

- **`SetReadOnly`** → `void`
  - `void SetReadOnly()`

#### 📁 SpecialDirectory.cs

- **`GetExistingDirectories`** → `IDictionary<Environment.SpecialFolder, SpecialDirectory>`
  - `IDictionary<Environment.SpecialFolder, SpecialDirectory> GetExistingDirectories()`

#### 📁 StringUtilities.cs

- **`WordToProperCase`** → `string`
  - String utility methods for advanced string processing.
  - `string WordToProperCase(string word)`

#### 📁 TypeExtensions.cs

- **`IsGenericTypeOf`** → `bool`
  - Gets the base types.
  - `bool IsGenericTypeOf(
#if NET9_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)`

- **`IsGenericTypeOf`** → `bool`
  - `bool IsGenericTypeOf(
#if NET9_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)`

#### 📁 VBConversion.cs

- **`Fix`** → `double`
  - Return the integer portion of a number.
  - `double Fix(double number)`

### 📊 Properties

- **`A4DatetimeMask`** : `string`
  - Provides set of default datetime values.
  - 📁 DateTimeDefaults.cs

- **`A4DtimeMask`** : `string`
  - Provides set of default datetime values.
  - 📁 DateTimeDefaults.cs

- **`AsyncHelper`** : `IAsyncHelper`
  - Gets the instance. ⚠️ This property must be initialized by calling before first use. ⚠️ Typically initialized automatically by FEx.Core during application startup.
  - 📁 FExAgnosticsStatics.cs

- **`Default`** : `DateTime`
  - Gets the default date.
  - 📁 DateTimeDefaults.cs

- **`DefaultCulture`** : `CultureInfo`
  - Provides set of default datetime values.
  - 📁 DateTimeDefaults.cs

- **`DefaultMaxDate`** : `DateTime`
  - Gets the default minimum date.
  - 📁 DateTimeDefaults.cs

- **`DefaultMinDate`** : `DateTime`
  - Gets the default minimum date.
  - 📁 DateTimeDefaults.cs

- **`IsodateMask`** : `string`
  - Provides set of default datetime values.
  - 📁 DateTimeDefaults.cs

- **`IsodatetimeMask`** : `string`
  - Provides set of default datetime values.
  - 📁 DateTimeDefaults.cs

- **`IsotimeMask`** : `string`
  - Provides set of default datetime values.
  - 📁 DateTimeDefaults.cs

- **`LettersAndNumbersRegex`** : `Regex`
  - The SQL wild card 'any value'.
  - 📁 StringExtensions.cs

- **`LettersNumbersAndUnderscoreRegex`** : `Regex`
  - The SQL wild card 'any value'.
  - 📁 StringExtensions.cs

- **`LettersRegex`** : `Regex`
  - The SQL wild card 'any value'.
  - 📁 StringExtensions.cs

- **`RomanNumeralsRegex`** : `Regex`
  - String utility methods for advanced string processing.
  - 📁 StringUtilities.cs

- **`SqlMax`** : `DateTime`
  - Gets the SQL maximum allowed date.
  - 📁 DateTimeDefaults.cs

- **`SqlMin`** : `DateTime`
  - Gets the SQL minimum allowed date.
  - 📁 DateTimeDefaults.cs

- **`UnixEpoch`** : `DateTime`
  - Provides set of default datetime values.
  - 📁 DateTimeDefaults.cs

- **`WordRegex`** : `Regex`
  - The SQL wild card 'any value'.
  - 📁 StringExtensions.cs

---

## FEx.Agnostics.TestMocks

**Namespace:** `Flakroup.FEx.AgnosticsTestMocks`  
**Classes:** 5 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 3 | **Properties:** 0

### 📦 Classes

- **`TestBase`**
- **`UniqueRandomGenerator`**
- **`XUnitFExLoggerHelper`**
- **`XUnitLogger`**
- **`XUnitLoggerProvider`**

### ⚙️ Public Methods

#### 📁 XUnitFExLoggerHelper.cs

- **`CreateMockLogger`** → `IFExLogger`
  - `IFExLogger CreateMockLogger(ITestOutputHelper output)`

#### 📁 XUnitLogger.cs

- **`IsEnabled`** → `bool`
  - `bool IsEnabled(LogLevel logLevel)`

#### 📁 XUnitLoggerProvider.cs

- **`CreateLogger`** → `ILogger`
  - `ILogger CreateLogger(string categoryName)`

---

## FEx.AppSettings

**Namespace:** `Flakroup.FEx.AppSettings`  
**Classes:** 9 | **Interfaces:** 3 | **Enums:** 0
**Extension Methods:** 2 | **Methods:** 4 | **Properties:** 0

### 🔌 Extension Methods

#### Extensions for `ApplicationSettingsBase`

- **`SaveAndReloadSettings`** → `void`
  - Saves the and reload settings.
  - `void SaveAndReloadSettings(this ApplicationSettingsBase settings)`
  - 📁 SettingsExtensions.cs

#### Extensions for `IConfigurationSection`

- **`BindJsonNet`** → `void`
  - `void BindJsonNet(this IConfigurationSection config,
                                   object instance,
                                   Func<string, string> jsonFunc = null)`
  - 📁 FExConfigurationExtensions.cs

### 🔷 Interfaces

- **`IBaseUserSettings`**
- **`IConfigurationService`**
- **`IFExAppSettingsModule`**

### 📦 Classes

- **`BaseUserSettings`**
- **`BaseUserSettings`**
- **`ConfigurationManagerExtensions`**
- **`ConfigurationService`**
- **`FExAppSettings`**
- **`FExAppSettingsModule`**
- **`FExConfigurationExtensions`**
- **`LegacyConfigurationProvider`**
- **`SettingsExtensions`** *(static)*

### ⚙️ Public Methods

#### 📁 BaseUserSettings.cs

- **`GetSettings`** → `T`
  - `T GetSettings(string persistencePath = null, bool isAsync = false)`

#### 📁 ConfigurationService.cs

- **`Build`** → `void`
  - `void Build(IEnumerable<IConfigurationSource> sources = null)`

- **`GetBoolSetting`** → `bool?`
  - `bool? GetBoolSetting(string key, bool? defaultValue = null)`

#### 📁 LegacyConfigurationProvider.cs

- **`Build`** → `IConfigurationProvider`
  - `IConfigurationProvider Build(IConfigurationBuilder builder)`

---

## FEx.Asyncx

**Description:** Async and parallel programming related FEx utilities

**Namespace:** `Flakroup.FEx.Asyncx`  
**Classes:** 13 | **Interfaces:** 1 | **Enums:** 0
**Extension Methods:** 1 | **Methods:** 10 | **Properties:** 1

### 🔌 Extension Methods

#### Extensions for `Func<Task>`

- **`WaitWithoutThreadLock`** → `void`
  - `void WaitWithoutThreadLock(this Func<Task> taskFactory,
                                             AsyncOptions options = AsyncOptions.ImmediateStart)`
  - 📁 JoinableTaskExtensions.cs

### 🔷 Interfaces

- **`IAsyncWorkerConfig`**

### 📦 Classes

- **`AsyncAutoInitializable`**
- **`AsyncBuffer`**
- **`AsyncInitializable`**
- **`AsyncProcessingQueue`**
- **`AsyncTaskCompletionSource`**
- **`AsyncWorker`**
- **`AsyncWorkersPool`**
- **`AsyncWorkersService`**
- **`FExAsyncx`**
- **`JoinableAsyncHelper`**
- **`JoinableTaskExtensions`**
- **`JoinableTaskFactoryHandler`**
- **`ManualResetEventAsync`** - An async manual reset event.

### ⚙️ Public Methods

#### 📁 AsyncInitializable.cs

- **`InitializeAsync`** → `Task`
  - If true doesn't wait for dependencies initialization
  - `Task InitializeAsync()`

#### 📁 AsyncProcessingQueue.cs

- **`Dispose`** → `void`
  - Called when a task completes so waiting tasks can be released.
  - `void Dispose()`

- **`EnqueueAsync`** → `Task`
  - Schedules a task in FIFO order.
  - `Task EnqueueAsync(Func<Task> taskFunc, CancellationToken cancellationToken = default)`

#### 📁 AsyncWorker.cs

- **`ExecuteTaskAsync`** → `Task<TResult>`
  - `Task<TResult> ExecuteTaskAsync(Func<T, Task<TResult>> func)`

#### 📁 AsyncWorkersPool.cs

- **`ExecuteOnPoolAsync`** → `Task<TResult>`
  - `Task<TResult> ExecuteOnPoolAsync(Func<TWorker, string, Task<TResult>> func,
                                                  Func<Guid, string> getId = null)`

#### 📁 JoinableAsyncHelper.cs

- **`GetFactory`** → `JoinableTaskFactoryHandler`
  - `JoinableTaskFactoryHandler GetFactory(Thread thread = null, bool replace = false)`

- **`SetMainJoinableTaskFactory`** → `void`
  - `void SetMainJoinableTaskFactory(Thread mainThread)`

#### 📁 ManualResetEventAsync.cs

- **`Reset`** → `void`
  - Reset the manual reset event.
  - `void Reset()`

- **`Set`** → `void`
  - Set the completion source.
  - `void Set()`

- **`WaitAsync`** → `Task<bool>`
  - Wait for the manual reset event.
  - `Task<bool> WaitAsync(TimeSpan? timeout = null, CancellationToken token = default)`

### 📊 Properties

- **`ConcurrencyLimit`** : `uint`
  - Dynamically updates the maximum allowed concurrency. When increasing, waiting tasks are released immediately in FIFO order.
  - 📁 AsyncProcessingQueue.cs

---

## FEx.Avaloniax

**Namespace:** `Flakroup.FEx.Avaloniax`  
**Classes:** 9 | **Interfaces:** 3 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 5 | **Properties:** 0

### 🔷 Interfaces

- **`IFExAvaloniaContainer`**
- **`IFExContainer`**
- **`INavigationService`**

### 📦 Classes

- **`AppViewLocator`**
- **`AvaloniaDispatcher`**
- **`AvaloniaExceptionHandler`**
- **`AvaloniaMessagePopupService`**
- **`FExAvaloniaApp`**
- **`FExAvaloniaReactiveUserControl`**
- **`FExAvaloniaViewModelBase`**
- **`FExAvaloniaxModule`**
- **`NavigationService`**

### ⚙️ Public Methods

#### 📁 AsyncInitializableViewModelBase.AsyncInitializable.cs

- **`BeginInitialization`** → `void`
  - If true doesn't wait for dependencies initialization
  - `void BeginInitialization(bool waitSynchronouslyForInitialization = false)`

- **`InitializeAsync`** → `Task`
  - If true doesn't wait for dependencies initialization
  - `Task InitializeAsync()`

- **`Reset`** → `void`
  - If true doesn't wait for dependencies initialization
  - `void Reset()`

#### 📁 AvaloniaDispatcher.cs

- **`CheckAccess`** → `bool`
  - `bool CheckAccess(object sender = null)`

#### 📁 AvaloniaExceptionHandler.cs

- **`Handle`** → `void`
  - `void Handle(Exception exception, IExceptionHandlerOptions options = null)`

---

## FEx.AzureStorage

**Namespace:** `Flakroup.FEx.AzureStorage`  
**Classes:** 6 | **Interfaces:** 1 | **Enums:** 1
**Extension Methods:** 4 | **Methods:** 7 | **Properties:** 0

### 🔌 Extension Methods

#### Extensions for `BlobItem`

- **`GetBlobChecksum`** → `string`
  - `string GetBlobChecksum(this BlobItem blob)`
  - 📁 BlobExtensions.cs

- **`GetBlobUri`** → `Uri`
  - `Uri GetBlobUri(this BlobItem blob, BlobContainerClient blobContainerClient)`
  - 📁 BlobExtensions.cs

#### Extensions for `CloudBlockBlob`

- **`GetBlobChecksum`** → `string`
  - `string GetBlobChecksum(this CloudBlockBlob blob)`
  - 📁 BlobExtensions.cs

- **`GetBlobUri`** → `Uri`
  - `Uri GetBlobUri(this CloudBlockBlob blob)`
  - 📁 BlobExtensions.cs

### 🔷 Interfaces

- **`IAzureStorageService`**

### 📦 Classes

- **`AzureStorageConfiguration`**
- **`AzureStorageService`**
- **`AzureStorageWorker`**
- **`BlobExtensions`**
- **`CloudBlockBlobInfo`**
- **`ProgressState`**

### 🔢 Enums

- **`StorageOperation`**

### ⚙️ Public Methods

#### 📁 AzureStorageService.cs

- **`Configure`** → `void`
  - The latest version according to https://docs.microsoft.com/en-us/rest/api/storageservices/versioning-for-the-azure-storage-services
  - `void Configure(string connStr, int parallelOperationsPerProcessorCount = 8)`

- **`GetBlobAsync`** → `Task<CloudBlockBlobInfo>`
  - `Task<CloudBlockBlobInfo> GetBlobAsync(string path,
                                                       CloudBlobContainer container = null,
                                                       string containerName = null,
                                                       CancellationToken cancellationToken = default)`

- **`GetCloudBlobContainer`** → `CloudBlobContainer`
  - `CloudBlobContainer GetCloudBlobContainer(string containerName)`

- **`ProcessBlobAsync`** → `Task<(string fileName, FileInfo localPath)>`
  - https://docs.microsoft.com/en-us/rest/api/storageservices/versioning-for-the-azure-storage-services
  - `Task<(string fileName, FileInfo localPath)> ProcessBlobAsync(
        string containerName,
        string downloadDir,
        string path)`

#### 📁 CloudBlockBlobInfo.cs

- **`FetchAttributesAsync`** → `Task`
  - Fetches the attributes asynchronous.
  - `Task FetchAttributesAsync(AccessCondition accessCondition = null,
                                           BlobRequestOptions options = null,
                                           OperationContext operationContext = null,
                                           CancellationToken cancellationToken = default)`

- **`GetMetadata`** → `string`
  - `string GetMetadata(string key)`

#### 📁 ProgressState.cs

- **`GetProgress`** → `string`
  - `string GetProgress(double prg)`

---

## FEx.CLI

**Namespace:** `Flakroup.FEx.CLI`  
**Classes:** 2 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 0 | **Properties:** 0

### 📦 Classes

- **`CommandLineParserHelper`**
- **`ErrorData`**

---

## FEx.Common

**Namespace:** `Flakroup.FEx.Common`  
**Classes:** 7 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 3 | **Properties:** 0

### 📦 Classes

- **`ConnectivityChangedSubject`**
- **`DeviceHelper`**
- **`FExBaseModule`**
- **`FExCommonModule`**
- **`FExInternetConnectionHelper`**
- **`MainThreadDispatcher`**
- **`SimpleMainThreadContextProvider`**

### ⚙️ Public Methods

#### 📁 FExInternetConnectionHelper.cs

- **`HasInternet`** → `bool`
  - `bool HasInternet(bool triggersCallbackOnLackOfInternet = true)`

#### 📁 MainThreadDispatcher.cs

- **`CheckAccess`** → `bool`
  - `bool CheckAccess(object sender = null)`

#### 📁 SimpleMainThreadContextProvider.cs

- **`SetMainThread`** → `void`
  - `void SetMainThread(bool throwOnNonMainThread = true)`

---

## FEx.Common.Abstractions

**Namespace:** `Flakroup.FEx.CommonAbstractions`  
**Classes:** 2 | **Interfaces:** 8 | **Enums:** 1
**Extension Methods:** 0 | **Methods:** 0 | **Properties:** 0

### 🔷 Interfaces

- **`IConnectivityChangedSubject`**
- **`IDeviceHelper`**
- **`IFExAppSettings`**
- **`IFExBaseContainer`**
- **`IFExCommonContainer`**
- **`IFExInternetConnectionHelper`**
- **`IStatusHub`**
- **`IStatusService`**

### 📦 Classes

- **`DateTimeProvider`** *(static)*
- **`DateTimeProviderContext`**

### 🔢 Enums

- **`FExNetworkAccess`** - Various states of the connection to the internet.

---

## FEx.Core

**Namespace:** `Flakroup.FEx.Core`  
**Classes:** 18 | **Interfaces:** 1 | **Enums:** 0
**Extension Methods:** 3 | **Methods:** 15 | **Properties:** 0

### 🔌 Extension Methods

#### Extensions for `object`

- **`SafeSerializeObject`** → `string`
  - `string SafeSerializeObject(this object initializeParameter)`
  - 📁 JsonExtensions.cs

#### Extensions for `object[]`

- **`ComputeMd5HashFromArguments`** → `string`
  - Generate MD5 hash from combined arguments
  - `string ComputeMd5HashFromArguments(this object[] arguments)`
  - 📁 StringExtensions.cs

#### Extensions for `string`

- **`ConvertWindowsToIanaTimeZone`** → `TimeZoneInfo`
  - `TimeZoneInfo ConvertWindowsToIanaTimeZone(this string timeZoneToId)`
  - 📁 DateTimeExtensions.cs

### 🔷 Interfaces

- **`IStackTraceFilter`**

### 📦 Classes

- **`ConcurrentHashSet`** - https://stackoverflow.com/questions/18922985/concurrent-hashsett-in-net-framework
- **`ConcurrentObservableDictionary`** - Based on https://github.com/ChadBurggraf/parallel-extensions-extras
- **`ConcurrentObservableList`**
- **`ConcurrentSortableObservableList`**
- **`DateTimeExtensions`** *(static)*
- **`EnumerableExtensions`**
- **`ExceptionHandler`** - Exception extensions class.
- **`FExArgumentlessSubject`**
- **`FExCoreModule`**
- **`JsonExtensions`**
- **`NavigationFlowSubject`**
- **`SafeContractResolver`**
- **`StackTraceFilter`**
- **`StackTraceFrame`**
- **`StackTraceGenerator`**
- **`StackTraceInfo`**
- **`StringExtensions`**
- **`TasksInfoSubject`**

### ⚙️ Public Methods

#### 📁 ConcurrentObservableDictionary.cs

- **`AddOrUpdate`** → `TValue`
  - Uses the specified functions to add a key/value pair to the if the key does not already exist, or to update a key/value pair in the if the key already exists.
  - `TValue AddOrUpdate(TKey key,
                              Func<TKey, TValue> addValueFactory,
                              Func<TKey, TValue, TValue> updateValueFactory)`

- **`GetEnumerator`** → `IDictionaryEnumerator`
  - `IDictionaryEnumerator GetEnumerator()`

- **`TryAdd`** → `bool`
  - Attempts to add the specified key and value to the .
  - `bool TryAdd(TKey key, TValue value)`

#### 📁 ConcurrentObservableList.cs

- **`Load`** → `void`
  - Clears the list and Loads the specified items.
  - `void Load(IEnumerable<T> items)`

- **`SuspendCount`** → `IDisposable`
  - Suspends count notifications.
  - `IDisposable SuspendCount()`

- **`SuspendNotifications`** → `IDisposable`
  - Suspends notifications. When disposed, a reset notification is fired.
  - `IDisposable SuspendNotifications()`

#### 📁 ExceptionHandler.cs

- **`Handle`** → `void`
  - Exception extensions class.
  - `void Handle(Exception exception, IExceptionHandlerOptions options = null)`

#### 📁 StackTraceFrame.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

#### 📁 StackTraceGenerator.cs (6 methods)

- **`Create`** → `MethodHandleAndILOffset[]`
  - `MethodHandleAndILOffset[] Create(IntPtr[] methods, int[] offsets)`

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

- **`GetStackTrace`** → `StackTrace`
  - `StackTrace GetStackTrace()`

- **`GetStackTrace`** → `StackTrace`
  - `StackTrace GetStackTrace()`

- **`GetStackTraceInfo`** → `StackTraceInfo`
  - `StackTraceInfo GetStackTraceInfo(string logger = null, string message = null)`

#### 📁 StackTraceInfo.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

---

## FEx.Core.Abstractions

**Namespace:** `Flakroup.FEx.CoreAbstractions`  
**Classes:** 33 | **Interfaces:** 15 | **Enums:** 6
**Extension Methods:** 13 | **Methods:** 41 | **Properties:** 11

### 🔌 Extension Methods

#### Extensions for `Assembly`

- **`HasResource`** → `bool`
  - `bool HasResource(this Assembly assembly,
                                   string resourceName,
                                   bool throwIfMissing = true,
                                   bool addNamespace = false)`
  - 📁 ReflectionHelper.cs

- **`ReadEmbeddedFile`** → `string`
  - `string ReadEmbeddedFile(this Assembly assembly,
                                          string resourceName,
                                          bool throwIfMissing = true,
                                          bool addNamespace = false)`
  - 📁 ReflectionHelper.cs

#### Extensions for `Exception`

- **`HandleException`** → `void`
  - `void HandleException(this Exception exception)`
  - 📁 ExceptionExtensions.cs

- **`HandleException`** → `void`
  - `void HandleException(this Exception exception, IExceptionHandlerOptions options)`
  - 📁 ExceptionExtensions.cs

- **`HandleException`** → `void`
  - `void HandleException(this Exception ex,
                                       bool informUser = false,
                                       bool wait = false,
                                       bool doNotReport = false,
                                       params (string, object)`
  - 📁 ExceptionExtensions.cs

#### Extensions for `object`

- **`AsDictionary`** → `IDictionary<string, object>`
  - `IDictionary<string, object> AsDictionary(this object source,
                                                           BindingFlags bindingAttr =
                                                               BindingFlags.DeclaredOnly
                                                               | BindingFlags.Public
                                                               | BindingFlags.Instance)`
  - 📁 ReflectionHelper.cs

- **`GetFieldValue`** → `object`
  - `object GetFieldValue(this object obj, string fieldName)`
  - 📁 ReflectionHelper.cs

- **`GetPropertyValue`** → `object`
  - `object GetPropertyValue(this object obj, string propertyName)`
  - 📁 ReflectionHelper.cs

- **`SetPropertyValue`** → `void`
  - `void SetPropertyValue(this object obj, string propertyName, object val)`
  - 📁 ReflectionHelper.cs

#### Extensions for `SynchronizationContext`

- **`PostInContext`** → `TaskCompletionSource<bool>`
  - `TaskCompletionSource<bool> PostInContext(this SynchronizationContext context,
                                                           Action action,
                                                           object sender,
                                                           Action<AttachedException> handleException = null)`
  - 📁 SynchronizationContextExtensions.cs

- **`SendInContext`** → `void`
  - `void SendInContext(this SynchronizationContext context, object sender, Action action)`
  - 📁 SynchronizationContextExtensions.cs

#### Extensions for `Thread`

- **`GetThreadSynchronizationContext`** → `SynchronizationContext`
  - `SynchronizationContext GetThreadSynchronizationContext(this Thread thread, bool createNew = false)`
  - 📁 SynchronizationContextExtensions.cs

- **`IsPlatformMainThread`** → `bool`
  - `bool IsPlatformMainThread(this Thread currentThread, bool? isUIApp = null)`
  - 📁 ThreadExtensions.cs

### 🔷 Interfaces

- **`IAppInfo`**
- **`IAppInfoProvider`**
- **`IAppThreadingSettings`**
- **`IAsyncInitializable`**
- **`IDeadlockMonitor`**
- **`IExceptionHandler`**
- **`IFExBehaviorSubject`**
- **`IFExCoreContainer`**
- **`IFExDispatcher`**
- **`IFExSubject`**
- **`IMainThreadContextProvider`**
- **`INavigationFlowSubject`**
- **`IResxManager`**
- **`IStackTraceProvider`**
- **`ITasksInfoSubject`**

### 📦 Classes

- **`ApiError`**
- **`AppThreadingSettings`**
- **`AppUtility`**
- **`AsyncHelper`**
- **`DeadlockMonitor`**
- **`DebugExceptionHandler`**
- **`DefaultDispatcher`**
- **`DefaultStackTraceProvider`**
- **`EventsExtensions`**
- **`ExceptionEventArgs`**
- **`ExceptionExtensions`**
- **`ExceptionHandlerBase`**
- **`ExceptionHandlerOptions`**
- **`FExBehaviorSubject`**
- **`FExCoreStatics`**
- **`FExDispatcher`**
- **`FExSubject`**
- **`MainThreadContextProvider`**
- **`ObservableExtensions`**
- **`ObservableHashSet`** - A hash set that implements the interfaces required for Entity Framework to use notification based change tracking for a collection navigation property.
- **`PlatformInfoProvider`** - Provides detailed information about the host operating system.
- **`ReflectionHelper`**
- **`StackError`**
- **`StackError`**
- **`StackTraceProvider`**
- **`SynchronizationContextExtensions`**
- **`SynchronizedAccessService`**
- **`TaskWrapper`**
- **`TaskWrapper`**
- **`TaskWrapperBase`**
- **`ThreadExtensions`**
- **`TimeoutError`**
- **`UriExtensions`**

### 🔢 Enums

- **`NavigationFlow`**
- **`OSEdition`**
- **`OSPlatformInfo`**
- **`OSProcessorArchitecture`**
- **`OSProduct`**
- **`SoftwareArchitecture`**

### ⚙️ Public Methods

#### 📁 AppUtility.cs

- **`GetOtherInstances`** → `int[]`
  - `int[] GetOtherInstances()`

- **`IsSingleInstance`** → `bool`
  - `bool IsSingleInstance()`

#### 📁 AsyncHelper.cs

- **`ExecuteDeferredTaskOnMainThreadAsync`** → `Task`
  - `Task ExecuteDeferredTaskOnMainThreadAsync(Action func,
                                                           AsyncOptions options = AsyncOptions.ImmediateStart)`

- **`ExecuteDeferredTaskOnMainThreadAsync`** → `Task`
  - `Task ExecuteDeferredTaskOnMainThreadAsync(Func<Task> func,
                                                           AsyncOptions options = AsyncOptions.ImmediateStart)`

#### 📁 DeadlockMonitor.cs

- **`Execute`** → `void`
  - `void Execute(Action action, StackTrace stackTrace = null, uint timeout = 3000)`

#### 📁 DefaultDispatcher.cs

- **`CheckAccess`** → `bool`
  - `bool CheckAccess(object sender = null)`

#### 📁 DefaultStackTraceProvider.cs

- **`GetStackTrace`** → `StackTrace`
  - `StackTrace GetStackTrace()`

#### 📁 ExceptionHandlerBase.cs

- **`Handle`** → `void`
  - `void Handle(Exception exception, IExceptionHandlerOptions options = null)`

#### 📁 FExCoreStatics.cs

- **`SetDefaults`** → `void`
  - `void SetDefaults()`

#### 📁 FExDispatcher.cs

- **`CheckAccess`** → `bool`
  - `bool CheckAccess(object sender = null)`

#### 📁 FExSubject.cs

- **`Dispose`** → `void`
  - Notifies the provider that an observer is to receive notifications.
  - `void Dispose()`

- **`Subscribe`** → `IDisposable`
  - Notifies the provider that an observer is to receive notifications.
  - `IDisposable Subscribe(IObserver<T> observer)`

#### 📁 MainThreadContextProvider.cs

- **`SetMainThread`** → `void`
  - `void SetMainThread(bool throwOnNonMainThread = true)`

#### 📁 ObservableHashSet.cs (19 methods)

- **`Add`** → `bool`
  - Adds the specified element to the hash set.
  - `bool Add(T item)`

- **`Clear`** → `void`
  - Removes all elements from the hash set.
  - `void Clear()`

- **`Contains`** → `bool`
  - Determines whether the hash set object contains the specified element.
  - `bool Contains(T item)`

- **`CopyTo`** → `void`
  - Copies the elements of the hash set to an array, starting at the specified array index.
  - `void CopyTo(T[] array, int arrayIndex)`

- **`CopyTo`** → `void`
  - Copies the specified number of elements of the hash set to an array, starting at the specified array index.
  - `void CopyTo([NotNull] T[] array, int arrayIndex, int count)`

- **`CopyTo`** → `void`
  - Copies the elements of the hash set to an array.
  - `void CopyTo([NotNull] T[] array)`

- **`ExceptWith`** → `void`
  - Removes all elements in the specified collection from the hash set.
  - `void ExceptWith(IEnumerable<T> other)`

- **`IntersectWith`** → `void`
  - Modifies the current hash set to contain only elements that are present in that object and in the specified collection.
  - `void IntersectWith(IEnumerable<T> other)`

- **`IsProperSubsetOf`** → `bool`
  - Determines whether the hash set is a proper subset of the specified collection.
  - `bool IsProperSubsetOf(IEnumerable<T> other)`

- **`IsProperSupersetOf`** → `bool`
  - Determines whether the hash set is a proper superset of the specified collection.
  - `bool IsProperSupersetOf(IEnumerable<T> other)`

- **`IsSubsetOf`** → `bool`
  - Determines whether the hash set is a subset of the specified collection.
  - `bool IsSubsetOf(IEnumerable<T> other)`

- **`IsSupersetOf`** → `bool`
  - Determines whether the hash set is a superset of the specified collection.
  - `bool IsSupersetOf(IEnumerable<T> other)`

- **`Overlaps`** → `bool`
  - Determines whether the current object and a specified collection share common elements.
  - `bool Overlaps(IEnumerable<T> other)`

- **`Remove`** → `bool`
  - Removes the specified element from the hash set.
  - `bool Remove(T item)`

- **`RemoveWhere`** → `int`
  - Removes all elements that match the conditions defined by the specified predicate from the hash set.
  - `int RemoveWhere([NotNull] Predicate<T> match)`

- **`SetEquals`** → `bool`
  - Determines whether the hash set and the specified collection contain the same elements.
  - `bool SetEquals(IEnumerable<T> other)`

- **`SymmetricExceptWith`** → `void`
  - Modifies the current hash set to contain only elements that are present either in that object or in the specified collection, but not both.
  - `void SymmetricExceptWith(IEnumerable<T> other)`

- **`TrimExcess`** → `void`
  - Sets the capacity of the hash set to the actual number of elements it contains, rounded up to a nearby, implementation-specific value.
  - `void TrimExcess()`

- **`UnionWith`** → `void`
  - Modifies the hash set to contain all elements that are present in itself, the specified collection, or both.
  - `void UnionWith(IEnumerable<T> other)`

#### 📁 OSVersion.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

#### 📁 PlatformInfoProvider.cs

- **`IsOSPlatform`** → `bool`
  - Indicates the operating-system platform.
  - `bool IsOSPlatform(string platform)`

- **`IsOSPlatform`** → `bool`
  - Indicates the operating-system platform.
  - `bool IsOSPlatform(string platform)`

#### 📁 StackTraceProvider.cs

- **`GetStackTrace`** → `StackTrace`
  - `StackTrace GetStackTrace()`

#### 📁 SynchronizationContextExtensions.cs

- **`Get`** → `SynchronizationContext`
  - `SynchronizationContext Get(bool createNew = false)`

#### 📁 TaskWrapper.cs

- **`SetResult`** → `void`
  - `void SetResult()`

- **`SetResult`** → `void`
  - `void SetResult(T result)`

#### 📁 TaskWrapperBase.cs

- **`SetException`** → `void`
  - `void SetException(Exception exception)`

- **`SetTask`** → `void`
  - `void SetTask(Func<TTask> task)`

### 📊 Properties

- **`BuildVersion`** : `int`
  - Gets the build version number of the operating system running on this computer.
  - 📁 PlatformInfoProvider.cs

- **`Edition`** : `OSEdition`
  - Determines if the current processor is 32 or 64-bit.
  - 📁 PlatformInfoProvider.cs

- **`InfoString`** : `string`
  - Gets the full version of the operating system running on this computer.
  - 📁 PlatformInfoProvider.cs

- **`IsWsl`** : `bool`
  - Indicates whether the current process is running under Windows Subsystem for Linux.
  - 📁 PlatformInfoProvider.cs

- **`Name`** : `string`
  - Gets the name of the operating system running on this computer.
  - 📁 PlatformInfoProvider.cs

- **`NoItems`** : `T[]`
  - PropertyChanged event (per ).
  - 📁 ObservableHashSet.cs

- **`OSBits`** : `SoftwareArchitecture`
  - Determines if the current application is 32 or 64-bit.
  - 📁 PlatformInfoProvider.cs

- **`ProcessorBits`** : `OSProcessorArchitecture`
  - Determines if the current processor is 32 or 64-bit.
  - 📁 PlatformInfoProvider.cs

- **`ProgramBits`** : `SoftwareArchitecture`
  - Determines if the current application is 32 or 64-bit.
  - 📁 PlatformInfoProvider.cs

- **`ServicePack`** : `string`
  - Gets the service pack information of the operating system running on this computer.
  - 📁 PlatformInfoProvider.cs

- **`Version`** : `Version`
  - Gets the full version of the operating system running on this computer.
  - 📁 PlatformInfoProvider.cs

---

## FEx.DependencyInjection

**Namespace:** `Flakroup.FEx.DependencyInjection`  
**Classes:** 6 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 11 | **Properties:** 0

### 📦 Classes

- **`ContainerExtensions`**
- **`FExDependencyInjectionModule`**
- **`FExMicrosoftDIServiceProvider`**
- **`FExServiceContainer`**
- **`FExStrongInjectServiceProvider`**
- **`ServiceProviderExtensions`** *(static)*

### ⚙️ Public Methods

#### 📁 FExMicrosoftDIServiceProvider.cs (6 methods)

- **`ConfigureServiceProviderAsync`** → `ValueTask`
  - Configure Microsoft DI service provider by discovering and running Microsoft DI specific modules.
  - `ValueTask ConfigureServiceProviderAsync()`

- **`CreateScope`** → `IScopeProvider`
  - Get service of type from the .
  - `IScopeProvider CreateScope()`

- **`CreateScope`** → `IServiceScope`
  - `IServiceScope CreateScope()`

- **`GetInstance`** → `object`
  - Get service of type from the .
  - `object GetInstance(Type serviceType)`

- **`GetRequiredService`** → `object`
  - Get service of type from the .
  - `object GetRequiredService(Type serviceType)`

- **`GetService`** → `object`
  - Get service of type from the .
  - `object GetService(Type serviceType)`

#### 📁 FExStrongInjectServiceProvider.cs

- **`ConfigureServiceProviderAsync`** → `ValueTask`
  - No-op for StrongInject provider as it doesn't need external engine configuration.
  - `ValueTask ConfigureServiceProviderAsync()`

- **`CreateScope`** → `IScopeProvider`
  - `IScopeProvider CreateScope()`

- **`GetInstance`** → `object`
  - `object GetInstance(Type serviceType)`

- **`GetRequiredService`** → `object`
  - `object GetRequiredService(Type serviceType)`

- **`GetService`** → `object`
  - No-op for StrongInject provider as it doesn't need external engine configuration.
  - `object GetService(Type serviceType)`

---

## FEx.DependencyInjection.Abstractions

**Namespace:** `Flakroup.FEx.DependencyInjectionAbstractions`  
**Classes:** 4 | **Interfaces:** 11 | **Enums:** 1
**Extension Methods:** 0 | **Methods:** 8 | **Properties:** 1

### 🔷 Interfaces

- **`IAnyConfigurator`**
- **`IAsyncConfigurator`**
- **`IConfigurator`**
- **`IFExDependencyInjectionContainer`**
- **`IFExServiceContainer`**
- **`IFExServiceProvider`**
- **`IFExStrongInjectServiceProvider`**
- **`IInitializeModule`**
- **`IMicrosoftDIConfigurator`**
- **`IMicrosoftDIConfigurator`**
- **`IScopeProvider`**

### 📦 Classes

- **`FExServiceProvider`**
- **`InitializeModule`**
- **`InitializeOnlyModule`**
- **`StaticsBase`**

### 🔢 Enums

- **`ConfigurationPriority`**

### ⚙️ Public Methods

#### 📁 FExServiceProvider.cs (7 methods)

- **`ConfigureServiceProviderAsync`** → `ValueTask`
  - No-op implementation for the static provider as it delegates to the actual providers.
  - `ValueTask ConfigureServiceProviderAsync()`

- **`CreateScope`** → `IScopeProvider`
  - Creates a scope for scoped services - not supported by StrongInject containers.
  - `IScopeProvider CreateScope()`

- **`Dispose`** → `void`
  - Disposes the service provider.
  - `void Dispose()`

- **`GetInstance`** → `object`
  - Retrieves the instance of the specified type.
  - `object GetInstance(Type serviceType)`

- **`GetRequiredService`** → `object`
  - Gets the required service object of the specified type.
  - `object GetRequiredService(Type serviceType)`

- **`GetService`** → `object`
  - Gets the service object of the specified type.
  - `object GetService(Type serviceType)`

- **`Release`** → `void`
  - Disposes the container.
  - `void Release()`

#### 📁 IScopeProvider.cs

- **`CreateScope`** → `IServiceScope`
  - `IServiceScope CreateScope()`

### 📊 Properties

- **`ServiceContainer`** : `IFExServiceContainer`
  - Retrieves the instance. ⚠️ This is discouraged and should only be used where Dependency Injection is unavailable.
  - 📁 FExServiceProvider.cs

---

## FEx.Downloader

**Namespace:** `Flakroup.FEx.Downloader`  
**Classes:** 13 | **Interfaces:** 7 | **Enums:** 1
**Extension Methods:** 0 | **Methods:** 25 | **Properties:** 8

### 🔷 Interfaces

- **`IDownloadBase`**
- **`IDownloadChunk`**
- **`IDownloadItem`**
- **`IDownloadPart`**
- **`IDownloadRange`**
- **`IDownloadStub`**
- **`IFExDownloaderModule`**

### 📦 Classes

- **`DownloadChunk`**
- **`DownloadIndex`**
- **`DownloadItem`**
- **`DownloadRange`**
- **`DownloadService`**
- **`DownloadStub`**
- **`FExDownloader`**
- **`FExDownloaderModule`**
- **`FlakHttpClient`**
- **`FlakWebClient`** - An extended WebClient that i.e. will store authentication cookie information and persist it through subsequent requests.
- **`HttpClientEx`**
- **`HttpClientService`**
- **`HttpClientServiceStub`**

### 🔢 Enums

- **`DownloadState`**

### ⚙️ Public Methods

#### 📁 DownloadChunk.cs

- **`CheckIfChunkFileIsFinished`** → `bool`
  - `bool CheckIfChunkFileIsFinished()`

#### 📁 DownloadIndex.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

#### 📁 DownloadItem.cs (7 methods)

- **`CancelAsync`** → `Task`
  - `Task CancelAsync()`

- **`CreateAsync`** → `Task<DownloadItem>`
  - `Task<DownloadItem> CreateAsync(IDownloadStub downloadItem,
                                                       bool reportProgress,
                                                       CancellationToken cancellationToken = default)`

- **`CreateAsync`** → `Task<DownloadItem>`
  - `Task<DownloadItem> CreateAsync(string url,
                                                       string path,
                                                       bool reportProgress,
                                                       WebRequestParams pars = null,
                                                       int parallelChunks = 50,
                                                       long dataLength = -1,
                                                       string md5Checksum = null,
                                                       CancellationToken cancellationToken = default)`

- **`CreateAsync`** → `Task<DownloadItem>`
  - `Task<DownloadItem> CreateAsync(Uri url,
                                                       string path,
                                                       bool reportProgress,
                                                       WebRequestParams pars = null,
                                                       int parallelChunks = 50,
                                                       long dataLength = -1,
                                                       string md5Checksum = null,
                                                       CancellationToken cancellationToken = default)`

- **`CreateFromResponse`** → `DownloadItem`
  - `DownloadItem CreateFromResponse(HttpWebResponse response,
                                                  string filePath,
                                                  bool reportProgress,
                                                  WebRequestParams pars = null,
                                                  long ping = 0,
                                                  int parallelChunks = 50,
                                                  string md5Checksum = null,
                                                  CancellationToken cancellationToken = default)`

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

- **`SetTemporaryCacheDirectory`** → `void`
  - `void SetTemporaryCacheDirectory(string path)`

#### 📁 DownloadRange.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

- **`SetContentRange`** → `void`
  - `void SetContentRange(string rangeHeader)`

#### 📁 DownloadStub.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

#### 📁 FlakHttpClient.cs (6 methods)

- **`CompareTo`** → `int`
  - Initializes a new instance of the class with a specific handler.
  - `int CompareTo(object obj)`

- **`CompareTo`** → `int`
  - Initializes a new instance of the class with a specific handler.
  - `int CompareTo(IDownloadBase other)`

- **`DelayAsync`** → `Task`
  - Initializes a new instance of the class with a specific handler.
  - `Task DelayAsync()`

- **`DoDownloadAsync`** → `Task`
  - Initializes a new instance of the class with a specific handler.
  - `Task DoDownloadAsync(string filePath, HttpResponseMessage response, bool lockOnFilePath = true)`

- **`Equals`** → `bool`
  - Initializes a new instance of the class with a specific handler.
  - `bool Equals(IDownloadBase other)`

- **`GetAsync`** → `Task<HttpResponseMessage>`
  - Initializes a new instance of the class with a specific handler.
  - `Task<HttpResponseMessage> GetAsync(Uri requestUri,
                                                    HttpCompletionOption completionOption,
                                                    CancellationToken cancellationToken)`

#### 📁 FlakWebClient.cs

- **`CookieMonster`** → `List<Cookie>`
  - Returns list of cookies.
  - `List<Cookie> CookieMonster()`

- **`DownloadFileWithProgressAsync`** → `Task`
  - Returns list of cookies.
  - `Task DownloadFileWithProgressAsync(Uri address, string filePath)`

#### 📁 HttpClientEx.cs

- **`CompareTo`** → `int`
  - Initializes a new instance of the class with a specific handler.
  - `int CompareTo(object obj)`

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

#### 📁 HttpClientService.cs

- **`GetInstanceAsync`** → `Task<HttpClientService>`
  - `Task<HttpClientService> GetInstanceAsync(string urlHost,
                                                                 WebRequestParams pars = null,
                                                                 int clientsCount = 2)`

- **`GetInstanceAsync`** → `Task<HttpClientService>`
  - `Task<HttpClientService> GetInstanceAsync(HttpClientServiceStub stub)`

#### 📁 HttpClientServiceStub.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

### 📊 Properties

- **`DownloadedFileAddress`** : `Uri`
  - An extended WebClient that i.e. will store authentication cookie information and persist it through subsequent requests.
  - 📁 FlakWebClient.cs

- **`HeadOnly`** : `bool`
  - An extended WebClient that i.e. will store authentication cookie information and persist it through subsequent requests.
  - 📁 FlakWebClient.cs

- **`Pars`** : `WebRequestParams`
  - An extended WebClient that i.e. will store authentication cookie information and persist it through subsequent requests.
  - 📁 FlakWebClient.cs

- **`ProgressInfo`** : `NotifyProgressInfoChanged`
  - An extended WebClient that i.e. will store authentication cookie information and persist it through subsequent requests.
  - 📁 FlakWebClient.cs

- **`RequestUri`** : `Uri`
  - An extended WebClient that i.e. will store authentication cookie information and persist it through subsequent requests.
  - 📁 FlakWebClient.cs

- **`ResponseUri`** : `Uri`
  - An extended WebClient that i.e. will store authentication cookie information and persist it through subsequent requests.
  - 📁 FlakWebClient.cs

- **`StatusCode`** : `HttpStatusCode`
  - An extended WebClient that i.e. will store authentication cookie information and persist it through subsequent requests.
  - 📁 FlakWebClient.cs

- **`Unreachable`** : `List<string>`
  - An extended WebClient that i.e. will store authentication cookie information and persist it through subsequent requests.
  - 📁 FlakWebClient.cs

---

## FEx.EFCore

**Namespace:** `Flakroup.FEx.EFCore`  
**Classes:** 18 | **Interfaces:** 9 | **Enums:** 1
**Extension Methods:** 4 | **Methods:** 10 | **Properties:** 0

### 🔌 Extension Methods

#### Extensions for `DbContextOptionsBuilder`

- **`ConfigureDbContext`** → `bool`
  - `bool ConfigureDbContext(this DbContextOptionsBuilder options,
                                          IFExDbConfig config,
                                          ISqlDbHelper sqlDbHelper)`
  - 📁 DbContextOptionsBuilderExtensions.cs

- **`UseSqlite`** → `void`
  - `void UseSqlite(this DbContextOptionsBuilder options, IFExDbConfig config)`
  - 📁 DbContextOptionsBuilderExtensions.cs

- **`UseSqlServer`** → `bool`
  - `bool UseSqlServer(this DbContextOptionsBuilder options,
                                    IFExDbConfig config,
                                    Action<SqlServerDbContextOptionsBuilder> configure = null)`
  - 📁 DbContextOptionsBuilderExtensions.cs

#### Extensions for `IEntityType`

- **`GetMappedProperties`** → `IReadOnlyCollection<string>`
  - `IReadOnlyCollection<string> GetMappedProperties(this IEntityType entityType)`
  - 📁 EntityTypeExtensions.cs

### 🔷 Interfaces

- **`IBulkDbConfig`**
- **`IBulkDbServiceBase`**
- **`IDbServiceBase`**
- **`IDbServiceConfig`**
- **`IEFCoreDatabaseBackedService`**
- **`IFExDbConfig`**
- **`IFExEFCoreModule`**
- **`IPooledDbService`**
- **`ISqlDbHelper`**

### 📦 Classes

- **`BulkDbConfig`**
- **`BulkDbServiceBase`**
- **`BulkOperationsExtensions`**
- **`ChangeInfo`**
- **`DbContextExtensions`**
- **`DbContextOptionsBuilderExtensions`**
- **`DbServiceBase`**
- **`DbServiceConfig`**
- **`EFCoreDatabaseBackedService`**
- **`EFCoreHelper`** *(static)*
- **`EntityTypeExtensions`**
- **`EntityValidationFail`**
- **`FExEFCore`**
- **`FExEFCoreModule`**
- **`PooledDbService`**
- **`ResilientTransaction`** - Use of an EF Core resiliency strategy when using multiple DbContexts within an explicit BeginTransaction(): See: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency
- **`SQLConnectionHelper`**
- **`SynchronizedDictionary`**

### 🔢 Enums

- **`SqlDialect`**

### ⚙️ Public Methods

#### 📁 FExDbConfig.cs

- **`ParseSqlConnectionString`** → `FExDbConfig`
  - `FExDbConfig ParseSqlConnectionString(string originalConnectionString)`

#### 📁 PooledDbService.cs

- **`MigrateAsync`** → `Task`
  - Migrate does the same job that EnsureCreated, but also adds table with migrations history
  - `Task MigrateAsync()`

- **`RunActionInDbContextAsync`** → `Task`
  - Migrate does the same job that EnsureCreated, but also adds table with migrations history
  - `Task RunActionInDbContextAsync(Action<TDbContext> func,
                                                string errorMessage = null,
                                                bool saveChanges = true,
                                                bool useTransaction = true)`

#### 📁 SQLConnectionHelper.cs

- **`CheckDbConnection`** → `bool`
  - `bool CheckDbConnection(string connectionString)`

- **`CheckDbConnectionAsync`** → `Task<bool>`
  - `Task<bool> CheckDbConnectionAsync(string connectionString,
                                                          CancellationToken cancellationToken = default)`

- **`CheckMasterDbConnection`** → `bool`
  - `bool CheckMasterDbConnection(IFExDbConfig config)`

- **`CheckMasterDbConnectionAsync`** → `Task<bool>`
  - `Task<bool> CheckMasterDbConnectionAsync(IFExDbConfig config,
                                                                CancellationToken cancellationToken = default)`

- **`GetConnectionString`** → `string`
  - `string GetConnectionString(IFExDbConfig config)`

#### 📁 SynchronizedDictionary.cs

- **`GetEnumerator`** → `IEnumerator<TValue>`
  - `IEnumerator<TValue> GetEnumerator()`

- **`GetOrAddValueAsync`** → `Task<TValue>`
  - `Task<TValue> GetOrAddValueAsync(TKey key, bool addNew = true, IDictionary<string, object> param = null)`

---

## FEx.Encryption

**Namespace:** `Flakroup.FEx.Encryption`  
**Classes:** 5 | **Interfaces:** 1 | **Enums:** 0
**Extension Methods:** 1 | **Methods:** 0 | **Properties:** 0

### 🔌 Extension Methods

#### Extensions for `string`

- **`Encode`** → `string`
  - `string Encode(this string valueToEncrypt)`
  - 📁 EncryptionHelper.cs

### 🔷 Interfaces

- **`IFExEncryptionSettings`**

### 📦 Classes

- **`EncryptionHelper`** *(static)*
- **`FExEncryption`**
- **`FExEncryptionModule`**
- **`SecureNotifyPropertyChanged`**
- **`StringHasher`**

---

## FEx.FileSystem

**Namespace:** `Flakroup.FEx.FileSystem`  
**Classes:** 4 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 13 | **Methods:** 10 | **Properties:** 8

### 🔌 Extension Methods

#### Extensions for `DirectoryInfo`

- **`AddToErrorPaths`** → `void`
  - `void AddToErrorPaths(this DirectoryInfo folder)`
  - 📁 DirectoryWalker.cs

- **`EnumerateDirectories`** → `IEnumerable<DirectoryInfo>`
  - Shim extension methods that allow the rest of the codebase (which expects ) to compile and work on older frameworks where that type does not exist (e.g. netstandard2.0/2.1). The implementation maps the subset of functionality that is possible to emulate via the existing overloads. Properties that have no equivalent are ignored.
  - `IEnumerable<DirectoryInfo> EnumerateDirectories(this DirectoryInfo dir,
                                                                  string searchPattern,
                                                                  FExEnumerationOptions options)`
  - 📁 DirectoryInfoEnumerationExtensions.cs

- **`EnumerateFiles`** → `IEnumerable<FileInfo>`
  - Shim extension methods that allow the rest of the codebase (which expects ) to compile and work on older frameworks where that type does not exist (e.g. netstandard2.0/2.1). The implementation maps the subset of functionality that is possible to emulate via the existing overloads. Properties that have no equivalent are ignored.
  - `IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo dir,
                                                       string searchPattern,
                                                       FExEnumerationOptions options)`
  - 📁 DirectoryInfoEnumerationExtensions.cs

- **`EnumerateFileSystemInfos`** → `IEnumerable<FileSystemInfo>`
  - Shim extension methods that allow the rest of the codebase (which expects ) to compile and work on older frameworks where that type does not exist (e.g. netstandard2.0/2.1). The implementation maps the subset of functionality that is possible to emulate via the existing overloads. Properties that have no equivalent are ignored.
  - `IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo dir,
                                                                       string searchPattern,
                                                                       FExEnumerationOptions options)`
  - 📁 DirectoryInfoEnumerationExtensions.cs

- **`GetDirectories`** → `DirectoryInfo[]`
  - Shim extension methods that allow the rest of the codebase (which expects ) to compile and work on older frameworks where that type does not exist (e.g. netstandard2.0/2.1). The implementation maps the subset of functionality that is possible to emulate via the existing overloads. Properties that have no equivalent are ignored.
  - `DirectoryInfo[] GetDirectories(this DirectoryInfo dir,
                                                 string searchPattern,
                                                 FExEnumerationOptions options)`
  - 📁 DirectoryInfoEnumerationExtensions.cs

- **`IsEmpty`** → `bool`
  - Given a list of folders, returns only those which are not subfolders of another folder in the list ("top-level" relative to each other).
  - `bool IsEmpty(this DirectoryInfo directory,
                               Func<IReadOnlyCollection<FileSystemInfo>, bool> predicate = null)`
  - 📁 DirectoryWalker.cs

- **`IsErrorPath`** → `bool`
  - `bool IsErrorPath(this DirectoryInfo folder)`
  - 📁 DirectoryWalker.cs

- **`IsLeaf`** → `bool`
  - `bool IsLeaf(this DirectoryInfo directory)`
  - 📁 DirectoryWalker.cs

- **`SafeDelete`** → `bool`
  - `bool SafeDelete(this DirectoryInfo folder, bool recursive = false, bool logDeletions = false)`
  - 📁 DirectoryWalker.cs

- **`SafeGetAllFiles`** → `List<FileInfo>`
  - Recursively gets all subdirectories from a root directory, ignoring any directories that throw an UnauthorizedAccessException or other IO exceptions.
  - `List<FileInfo> SafeGetAllFiles(this DirectoryInfo root,
                                                 FileFilterDelegate predicate = null,
                                                 string searchPattern = "*",
                                                 FExEnumerationOptions options = null)`
  - 📁 DirectoryWalker.cs

#### Extensions for `FileInfo`

- **`SafeDelete`** → `bool`
  - `bool SafeDelete(this FileInfo file, bool logDeletions = false)`
  - 📁 DirectoryWalker.cs

- **`VerifyGZip`** → `bool`
  - `bool VerifyGZip(this FileInfo fileToDecompress)`
  - 📁 CompressionHelper.cs

#### Extensions for `IList<DirectoryInfo>`

- **`GetTopLevelFolders`** → `List<DirectoryInfo>`
  - Given a list of folders, returns only those which are not subfolders of another folder in the list ("top-level" relative to each other).
  - `List<DirectoryInfo> GetTopLevelFolders(this IList<DirectoryInfo> folders, bool logFindings = false)`
  - 📁 DirectoryWalker.cs

### 📦 Classes

- **`CompressionHelper`**
- **`DirectoryWalker`**
- **`FExEnumerationOptions`** - Provides file and directory enumeration options.
- **`FileSystemUtilities`**

### ⚙️ Public Methods

#### 📁 FExEnumerationOptions.cs

- **`ToEnumerationOptions`** → `EnumerationOptions`
  - Converts current instance to the runtime which is supported on net6.0+ and therefore on net9.0 that this solution targets.
  - `EnumerationOptions ToEnumerationOptions()`

- **`ToEnumerationOptions`** → `object`
  - Implicit conversion operator so the wrapper can be passed directly where is expected.
  - `object ToEnumerationOptions()`

#### 📁 FileSystemUtilities.cs (8 methods)

- **`CreateMissingDestinationDirectories`** → `Result<Error>`
  - `Result<Error> CreateMissingDestinationDirectories(FileSystemInfo dest,
                                                                    ICollection<string> sourcePaths)`

- **`FixFileName`** → `string`
  - Fixes the name of the file.
  - `string FixFileName(string fileName)`

- **`GetEncoding`** → `Encoding`
  - Gets file encoding with or without Byte Order Mark
  - `Encoding GetEncoding(string path, bool omitBom = false)`

- **`GetSourceFiles`** → `ConcurrentList<FileInfo>`
  - `ConcurrentList<FileInfo> GetSourceFiles(
        ConcurrentDictionary<DirectoryInfo, FileInfo[]> sourceDirectories)`

- **`IsPathFile`** → `Result<bool, ExceptionError>`
  - Fixes the name of the file.
  - `Result<bool, ExceptionError> IsPathFile(string path)`

- **`OmitBom`** → `Encoding`
  - Omits the bom.
  - `Encoding OmitBom(Encoding enc)`

- **`ProcessDirectory`** → `Result<AggregatedError>`
  - `Result<AggregatedError> ProcessDirectory(string source,
                                                           string dest,
                                                           FileOperation fileOperation,
                                                           bool printPaths = true,
                                                           bool printLog = true,
                                                           params string[] exclusionPaths)`

- **`ProcessDirectory`** → `Result<AggregatedError>`
  - `Result<AggregatedError> ProcessDirectory(DirectoryInfo sourceInfo,
                                                           DirectoryInfo dest,
                                                           FileOperation fileOperation,
                                                           bool printPaths = true,
                                                           bool printLog = true,
                                                           params string[] exclusionPaths)`

### 📊 Properties

- **`AttributesToSkip`** : `FileAttributes`
  - Gets or sets the attributes to skip. The default is <c>FileAttributes.Hidden | FileAttributes.System</c>.
  - 📁 FExEnumerationOptions.cs

- **`BufferSize`** : `int`
  - Gets or sets the suggested buffer size, in bytes. The default is 0 (no suggestion).
  - 📁 FExEnumerationOptions.cs

- **`CaseSensitive`** : `bool`
  - Gets or sets whether to use case-sensitive matching.
  - 📁 FExEnumerationOptions.cs

- **`IgnoreInaccessible`** : `bool`
  - Gets or sets a value that indicates whether to skip files or directories when access is denied (for example, or ). The default is .
  - 📁 FExEnumerationOptions.cs

- **`MaxRecursionDepth`** : `int`
  - Gets or sets a value that indicates the maximum directory depth to recurse while enumerating, when is set to .
  - 📁 FExEnumerationOptions.cs

- **`RecurseSubdirectories`** : `bool`
  - Gets or sets a value that indicates whether to recurse into subdirectories while enumerating. The default is .
  - 📁 FExEnumerationOptions.cs

- **`ReturnSpecialDirectories`** : `bool`
  - Gets or sets a value that indicates whether to return the special directory entries "." and "..".
  - 📁 FExEnumerationOptions.cs

- **`UseSimpleMatching`** : `bool`
  - Gets or sets whether to use simple wildcard matching.
  - 📁 FExEnumerationOptions.cs

---

## FEx.Flurlx

**Namespace:** `Flakroup.FEx.Flurlx`  
**Classes:** 11 | **Interfaces:** 3 | **Enums:** 1
**Extension Methods:** 4 | **Methods:** 11 | **Properties:** 8

### 🔌 Extension Methods

#### Extensions for `HttpContent`

- **`StripCharsetQuotes`** → `HttpContent`
  - `HttpContent StripCharsetQuotes(this HttpContent content)`
  - 📁 FlurlExtensions.cs

#### Extensions for `IFlurlRequest`

- **`FixBooleanQueryParameters`** → `IFlurlRequest`
  - `IFlurlRequest FixBooleanQueryParameters(this IFlurlRequest req)`
  - 📁 FlurlExtensions.cs

#### Extensions for `IFlurlResponse`

- **`IsSuccessStatusCode`** → `bool`
  - Gets a value that indicates if the HTTP response was successful.
  - `bool IsSuccessStatusCode(this IFlurlResponse response)`
  - 📁 FlurlResponseExtensions.cs

#### Extensions for `Url`

- **`FixBooleanQueryParameters`** → `Url`
  - `Url FixBooleanQueryParameters(this Url url)`
  - 📁 FlurlExtensions.cs

### 🔷 Interfaces

- **`IApiConfiguration`**
- **`IFExFlurlxContainer`**
- **`IFlurlConfigurator`**

### 📦 Classes

- **`FExCookieJar`** - A collection of FlurlCookies that can be attached to one or more FlurlRequests, either explicitly via WithCookies or implicitly via a CookieSession. Stores cookies received via Set-Cookie response headers.
- **`FExFlurlx`**
- **`FExFlurlxModule`**
- **`FExInvalidCookieException`** - Exception thrown when attempting to add or update an invalid FlurlCookie to a CookieJar.
- **`FExPollyPolicyBuilder`** - FEx Polly Policy Builder - Creates comprehensive resilience policies for HTTP requests.
- **`FlurlApiBase`** - Abstract base class for building typed API clients with Polly resilience.
- **`FlurlConfigurator`**
- **`FlurlExtensions`**
- **`FlurlResponseExtensions`** *(static)*
- **`PollyPolicyConfiguration`** - Configuration for Polly resilience policies.
- **`UrlExtensions`**

### 🔢 Enums

- **`RequestMethod`** - HTTP method to use when making requests

### ⚙️ Public Methods

#### 📁 FExCookieJar.cs (6 methods)

- **`AddOrReplace`** → `FExCookieJar`
  - Adds a cookie to the jar or replaces one with the same Name/Domain/Path. Throws FExInvalidCookieException if cookie is invalid.
  - `FExCookieJar AddOrReplace(string name, object value, string originUrl, DateTimeOffset? dateReceived = null)`

- **`AddOrReplace`** → `FExCookieJar`
  - Adds a cookie to the jar or replaces one with the same Name/Domain/Path. Throws FExInvalidCookieException if cookie is invalid.
  - `FExCookieJar AddOrReplace(FlurlCookie cookie)`

- **`Clear`** → `FExCookieJar`
  - Removes all cookies from this CookieJar
  - `FExCookieJar Clear()`

- **`GetEnumerator`** → `IEnumerator<FlurlCookie>`
  - A collection of FlurlCookies that can be attached to one or more FlurlRequests, either explicitly via WithCookies or implicitly via a CookieSession. Stores cookies received via Set-Cookie response headers.
  - `IEnumerator<FlurlCookie> GetEnumerator()`

- **`Remove`** → `FExCookieJar`
  - Removes all cookies matching the given predicate.
  - `FExCookieJar Remove(Func<FlurlCookie, bool> predicate)`

- **`TryAddOrReplace`** → `bool`
  - Adds a cookie to the jar or updates if one with the same Name/Domain/Path already exists, but only if it is valid and not expired.
  - `bool TryAddOrReplace(FlurlCookie cookie, out string reason)`

#### 📁 FExPollyPolicyBuilder.cs

- **`BuildFullSuitePolicy`** → `IAsyncPolicy<HttpResponseMessage>`
  - Builds a comprehensive resilience policy with all Polly features.
  - `IAsyncPolicy<HttpResponseMessage> BuildFullSuitePolicy(PollyPolicyConfiguration config)`

#### 📁 FlurlConfigurator.cs

- **`GetClient`** → `IFlurlClient`
  - `IFlurlClient GetClient()`

- **`GetResiliencePolicy`** → `IAsyncPolicy<HttpResponseMessage>`
  - `IAsyncPolicy<HttpResponseMessage> GetResiliencePolicy()`

#### 📁 PollyPolicyConfiguration.cs

- **`FastApiDefaults`** → `PollyPolicyConfiguration`
  - Creates a default configuration optimized for fast, reliable APIs.
  - `PollyPolicyConfiguration FastApiDefaults()`

- **`SlowApiDefaults`** → `PollyPolicyConfiguration`
  - Creates a default configuration optimized for slow APIs (e.g., Synology DSM).
  - `PollyPolicyConfiguration SlowApiDefaults()`

### 📊 Properties

- **`CircuitBreakerDuration`** : `TimeSpan`
  - Duration the circuit breaker stays open before allowing test requests. Default: 1 minute
  - 📁 PollyPolicyConfiguration.cs

- **`CircuitBreakerFailureThreshold`** : `int`
  - Number of consecutive failures before opening the circuit breaker. Default: 5 failures
  - 📁 PollyPolicyConfiguration.cs

- **`EnableFallback`** : `bool`
  - Enable fallback policy for graceful degradation (e.g., returning cached data). Default: true
  - 📁 PollyPolicyConfiguration.cs

- **`InitialRetryDelay`** : `TimeSpan`
  - Initial delay before first retry. Subsequent retries use exponential backoff. Default: 1 second
  - 📁 PollyPolicyConfiguration.cs

- **`MaxParallelization`** : `int`
  - Maximum number of parallel requests allowed (Bulkhead limit). Essential for slow APIs to prevent overwhelming the endpoint. Default: 10 concurrent requests
  - 📁 PollyPolicyConfiguration.cs

- **`MaxQueuingActions`** : `int`
  - Maximum number of requests that can be queued when Bulkhead limit is reached. Default: 20 queued actions
  - 📁 PollyPolicyConfiguration.cs

- **`MaxRetryAttempts`** : `int`
  - Maximum number of retry attempts before giving up. Default: 3 attempts
  - 📁 PollyPolicyConfiguration.cs

- **`RequestTimeout`** : `TimeSpan`
  - Maximum timeout for a single HTTP request. Default: 30 seconds
  - 📁 PollyPolicyConfiguration.cs

---

## FEx.FTPx

**Namespace:** `Flakroup.FEx.FTPx`  
**Classes:** 3 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 11 | **Properties:** 1

### 📦 Classes

- **`FtpClientFactory`**
- **`FtpCommon`**
- **`FtpDownloader`** - FTP download utilities with resume support.

### ⚙️ Public Methods

#### 📁 FtpClientFactory.cs

- **`CreateAsync`** → `Task<FtpClient>`
  - `Task<FtpClient> CreateAsync(string user = null, string pass = null, bool useProxy = false, int port = 0)`

- **`CreateAsync`** → `Task<FtpClient>`
  - `Task<FtpClient> CreateAsync(string user = null, string pass = null, ProxyInfo proxy = null, int port = 0)`

- **`CreateAsync`** → `Task<FtpClient>`
  - `Task<FtpClient> CreateAsync(NetworkCredential credentials = null, bool useProxy = false, int port = 0)`

- **`CreateAsync`** → `Task<FtpClient>`
  - `Task<FtpClient> CreateAsync(NetworkCredential credentials = null, ProxyInfo proxy = null, int port = 0)`

- **`GetInstanceAsync`** → `Task<FtpClientFactory>`
  - `Task<FtpClientFactory> GetInstanceAsync(string hostUri, int maxParallel = 5)`

#### 📁 FtpCommon.cs

- **`CreateAsync`** → `Task<FtpClient>`
  - `Task<FtpClient> CreateAsync(Uri ftphost, string username, string password, bool useProxy = false, int port = 0)`

- **`GetFtpFileInfoAsync`** → `Task<FtpListItem>`
  - `Task<FtpListItem> GetFtpFileInfoAsync(string ftpfilepath, Uri ftphost, string username, string password, bool useProxy = false, int port = 0)`

- **`GetListingAsync`** → `Task<FtpListItem[]>`
  - `Task<FtpListItem[]> GetListingAsync(string ftpdirpath, Uri ftphost, string username, string password, bool useProxy = false)`

#### 📁 FtpDownloader.cs

- **`CalculateSizeAsync`** → `Task<double>`
  - Calculates the size.
  - `Task<double> CalculateSizeAsync(Uri serverUri,
                                                        bool promptOnError = true,
                                                        LengthType unit = LengthType.Megabytes,
                                                        string username = "",
                                                        string password = "")`

- **`DownloadFileAsync`** → `Task<bool>`
  - FTP download utilities with resume support.
  - `Task<bool> DownloadFileAsync(string fileName,
                                                     Uri serverUri,
                                                     IProgressAggregator viewModel,
                                                     string username = "",
                                                     string password = "")`

- **`RestartDownloadFromServerAsync`** → `Task<bool>`
  - Restarts the download from server.
  - `Task<bool> RestartDownloadFromServerAsync(string fileName,
                                                                  Uri serverUri,
                                                                  IProgressAggregator viewModel,
                                                                  long offset = 0,
                                                                  string username = "",
                                                                  string password = "")`

### 📊 Properties

- **`StatusDescription`** : `string`
  - FTP download utilities with resume support.
  - 📁 FtpDownloader.cs

---

## FEx.Json

**Namespace:** `Flakroup.FEx.Json`  
**Classes:** 10 | **Interfaces:** 1 | **Enums:** 0
**Extension Methods:** 7 | **Methods:** 5 | **Properties:** 0

### 🔌 Extension Methods

#### Extensions for `FileInfo`

- **`SerializeToFile`** → `void`
  - `void SerializeToFile(this FileInfo file,
                                       object self,
                                       JsonSerializerSettings settings = null,
                                       Formatting formatting = Formatting.None)`
  - 📁 JsonExtensions.cs

#### Extensions for `object`

- **`ToJson`** → `string`
  - `string ToJson(this object self,
                                JsonSerializerSettings settings = null,
                                Formatting formatting = Formatting.None)`
  - 📁 JsonExtensions.cs

#### Extensions for `Stream`

- **`DeserializeFromStream`** → `object`
  - `object DeserializeFromStream(this Stream stream, JsonSerializerSettings settings = null)`
  - 📁 JsonExtensions.cs

#### Extensions for `string`

- **`FromJson`** → `object`
  - `object FromJson(this string json, JsonSerializerSettings settings = null)`
  - 📁 JsonExtensions.cs

- **`PrettyPrintJson`** → `string`
  - `string PrettyPrintJson(this string json,
                                         JsonLoadSettings loadSettings = null,
                                         JsonSerializerSettings saveSettings = null,
                                         Formatting formatting = Formatting.Indented)`
  - 📁 JsonExtensions.cs

- **`ReformatJson`** → `string`
  - Reformats the json.
  - `string ReformatJson(this string json)`
  - 📁 JsonExtensions.cs

- **`TrimJsonString`** → `string`
  - Deserializes the token.
  - `string TrimJsonString(this string jsonValue)`
  - 📁 JsonExtensions.cs

### 🔷 Interfaces

- **`IFExJsonContainer`**

### 📦 Classes

- **`DIContractResolver`**
- **`DIMeta`**
- **`FExJson`**
- **`FExJsonModule`**
- **`JsonExtensions`**
- **`JsonPathConverter`**
- **`NotifyPropertyChangedExtensions`**
- **`ParseStringToDoubleConverter`**
- **`ParseStringToLongConverter`**
- **`PropertyRenameAndIgnoreSerializerContractResolver`**

### ⚙️ Public Methods

#### 📁 DIMeta.cs

- **`IsRegistred`** → `bool`
  - `bool IsRegistred(Type t)`

#### 📁 JsonExtensions.cs

- **`PrettyPrintFile`** → `void`
  - Deserializes the token.
  - `void PrettyPrintFile(string orgPath, string destPath)`

#### 📁 JsonPathConverter.cs

- **`CanConvert`** → `bool`
  - `bool CanConvert(Type objectType)`

#### 📁 ParseStringToDoubleConverter.cs

- **`CanConvert`** → `bool`
  - `bool CanConvert(Type t)`

#### 📁 ParseStringToLongConverter.cs

- **`CanConvert`** → `bool`
  - `bool CanConvert(Type t)`

---

## FEx.KeyVault

**Namespace:** `Flakroup.FEx.KeyVault`  
**Classes:** 2 | **Interfaces:** 3 | **Enums:** 0
**Extension Methods:** 2 | **Methods:** 0 | **Properties:** 4

### 🔌 Extension Methods

#### Extensions for `IConfigurationBuilder`

- **`AddAzureKeyVaultWithCertificate`** → `void`
  - `void AddAzureKeyVaultWithCertificate(this IConfigurationBuilder config,
                                                       IKeyVaultByCertCredentials credentials)`
  - 📁 KeyVaultConfigurator.cs

- **`AddAzureKeyVaultWithClientSecret`** → `void`
  - `void AddAzureKeyVaultWithClientSecret(this IConfigurationBuilder config,
                                                        IKeyVaultByClientSecretCredentials credentials)`
  - 📁 KeyVaultConfigurator.cs

### 🔷 Interfaces

- **`IKeyVaultByCertCredentials`**
- **`IKeyVaultByClientSecretCredentials`**
- **`IKeyVaultCredentials`**

### 📦 Classes

- **`KeyVaultConfigurator`**
- **`KeyVaultCredentials`**

### 📊 Properties

- **`AzureADCertThumbprint`** : `string`
  - Gets or sets the Application (client) ID
  - 📁 KeyVaultCredentials.cs

- **`AzureADClientId`** : `string`
  - Gets or sets the Application (client) ID
  - 📁 KeyVaultCredentials.cs

- **`AzureADClientSecret`** : `string`
  - Gets or sets the Application (client) ID
  - 📁 KeyVaultCredentials.cs

- **`AzureADTenantId`** : `string`
  - Gets or sets the Directory (tenant) ID
  - 📁 KeyVaultCredentials.cs

---

## FEx.Legacy

**Namespace:** `Flakroup.FEx.Legacy`  
**Classes:** 17 | **Interfaces:** 10 | **Enums:** 1
**Extension Methods:** 2 | **Methods:** 21 | **Properties:** 7

### 🔌 Extension Methods

#### Extensions for `JobSpecs?`

- **`HasFlagFast`** → `bool`
  - `bool HasFlagFast(this JobSpecs? value, JobSpecs flag)`
  - 📁 JobSpecs.cs

- **`HasFlagsFast`** → `bool`
  - `bool HasFlagsFast(this JobSpecs? value, params JobSpecs[] flags)`
  - 📁 JobSpecs.cs

### 🔷 Interfaces

- **`ICachedImageStorage`**
- **`IFExLegacyContainer`**
- **`IIndexEntryBase`**
- **`IProgressListenerViewModel`**
- **`IProgressStateGet`**
- **`IProgressSubscriber`**
- **`IRunAsync`**
- **`IRunAsyncView`**
- **`ITasksHandler`**
- **`IThreadingAwareViewModel`**

### 📦 Classes

- **`DelayedNotificationObserver`**
- **`FExLegacy`**
- **`FExLegacyModule`**
- **`JobSpecsExtensions`**
- **`MappedDriveResolver`**
- **`PassInfoEventArgs`**
- **`ProducerConsumerCollectionBase`** - Provides a base implementation for producer-consumer collections that wrap other producer-consumer collections. Based on https://github.com/ChadBurggraf/parallel-extensions-extras
- **`ProgressAggregatorViewModel`**
- **`ProgressListenerViewModel`**
- **`SectorInfoEventArgs`** - Get the total number of passes to be run
- **`SubscriptionActions`**
- **`TasksHandler`**
- **`ViewModelBaseExtensions`**
- **`WebServices`**
- **`Wipe`** - https://www.codeproject.com/Articles/22736/Securely-Delete-a-File-using-NET
- **`WipeDoneEventArgs`** - Get the total number of sectors to be run
- **`WipeErrorEventArgs`** - Get the total number of sectors to be run

### 🔢 Enums

- **`JobSpecs`**

### ⚙️ Public Methods

#### 📁 MappedDriveResolver.cs

- **`CheckUncPath`** → `string`
  - Given a local mapped drive letter, determine if it is a network drive. If so, return the server share.
  - `string CheckUncPath(string mappedDrive)`

- **`GetDriveLetter`** → `string`
  - Given a path will extract just the drive letter with volume separator.
  - `string GetDriveLetter(string path)`

- **`IsNetworkDrive`** → `bool`
  - Checks if the given path is a network drive.
  - `bool IsNetworkDrive(string path)`

- **`ResolveToRootUnc`** → `string`
  - Resolves the given path to a root UNC path if the path is a mapped drive. Otherwise, just returns the given path.
  - `string ResolveToRootUnc(string path)`

- **`ResolveToUnc`** → `string`
  - Resolves the given path to a full UNC path if the path is a mapped drive. Otherwise, just returns the given path.
  - `string ResolveToUnc(string path)`

#### 📁 ProducerConsumerCollectionBase.cs

- **`CopyTo`** → `void`
  - Copies the contents of the collection to an array.
  - `void CopyTo(T[] array, int index)`

- **`GetEnumerator`** → `IEnumerator<T>`
  - Gets an enumerator for the collection.
  - `IEnumerator<T> GetEnumerator()`

- **`ToArray`** → `T[]`
  - Creates an array containing the contents of the collection.
  - `T[] ToArray()`

#### 📁 SubscriptionActions.cs

- **`GetSubscription`** → `IDisposable`
  - `IDisposable GetSubscription(IObservable<T> observable, T subscriptionArgument = default)`

#### 📁 ThreadingAwareViewModel.AsyncInitializable.cs

- **`BeginInitialization`** → `void`
  - If true doesn't wait for dependencies initialization
  - `void BeginInitialization(bool waitSynchronouslyForInitialization = false)`

- **`InitializeAsync`** → `Task`
  - If true doesn't wait for dependencies initialization
  - `Task InitializeAsync()`

- **`Reset`** → `void`
  - If true doesn't wait for dependencies initialization
  - `void Reset()`

#### 📁 WebServices.cs (8 methods)

- **`ConvertXmlToJson`** → `string`
  - Converts the XML to json.
  - `string ConvertXmlToJson(string responseBody)`

- **`GetHttpResponseAsync`** → `Task<HttpResponseMessage>`
  - `Task<HttpResponseMessage> GetHttpResponseAsync(Func<Task<HttpResponseMessage>> responseFunc)`

- **`GetRequestResultAsync`** → `Task<string>`
  - Handles GET requests.
  - `Task<string> GetRequestResultAsync(string requestUrl,
                                                           ICredentials credentials = null,
                                                           bool resultAsJson = true,
                                                           List<HttpStatusCode> ommitCodes = null,
                                                           List<Cookie> cookies = null)`

- **`GetWebApiResponseCodeInfo`** → `string`
  - Translates to API status message.
  - `string GetWebApiResponseCodeInfo(HttpStatusCode statusCode)`

- **`GetWebApiResponseCodeInfo`** → `string`
  - Translates to API status message.
  - `string GetWebApiResponseCodeInfo(int statusCode)`

- **`HandleResponseAsync`** → `Task<ResponseResult>`
  - Handles the response.
  - `Task<ResponseResult> HandleResponseAsync(string requestUrl,
                                                                 List<HttpStatusCode> ommitCodes,
                                                                 Func<Task<HttpResponseMessage>> responseHandler)`

- **`PostRequestResultAsync`** → `Task<ResponseResult>`
  - Handles POST requests.
  - `Task<ResponseResult> PostRequestResultAsync(string requestUrl,
                                                                    string postContent,
                                                                    ICredentials credentials = null,
                                                                    List<HttpStatusCode> ommitCodes = null,
                                                                    List<Cookie> cookies = null)`

- **`PrepareHttpClient`** → `HttpClient`
  - Prepares the HTTP client.
  - `HttpClient PrepareHttpClient(string address,
                                               ICredentials credentials,
                                               bool resultAsJson,
                                               IEnumerable<Cookie> cookies = null)`

#### 📁 Wipe.cs

- **`WipeFile`** → `void`
  - Deletes a file in a secure way by overwriting it with random garbage data n times.
  - `void WipeFile(string filename, int timesToWrite)`

### 📊 Properties

- **`CurrentPass`** : `int`
  - Get the current pass
  - 📁 Wipe.cs

- **`CurrentSector`** : `int`
  - Get the current sector
  - 📁 Wipe.cs

- **`Instance`** : `Wipe`
  - https://www.codeproject.com/Articles/22736/Securely-Delete-a-File-using-NET
  - 📁 Wipe.cs

- **`IsUiUnlocked`** : `bool`
  - Gets or sets a value indicating whether View instance related with this ViewModel is unlocked.
  - 📁 ThreadingAwareViewModel.cs

- **`TotalPasses`** : `int`
  - Get the total number of passes to be run
  - 📁 Wipe.cs

- **`TotalSectors`** : `int`
  - Get the total number of sectors to be run
  - 📁 Wipe.cs

- **`WipeError`** : `Exception`
  - Get the total number of sectors to be run
  - 📁 Wipe.cs

---

## FEx.Logging

**Namespace:** `Flakroup.FEx.Logging`  
**Classes:** 10 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 8 | **Properties:** 0

### 📦 Classes

- **`AsyncFileSinkConfigurator`**
- **`ConsoleSinkConfigurator`**
- **`DebugSinkConfigurator`**
- **`DefaultPlatformLogger`**
- **`FExLoggingConfigurator`**
- **`FExLoggingModule`**
- **`FExLoggingService`**
- **`FExSerilogLogger`**
- **`LoggingConfiguration`**
- **`PlatformSinkConfigurator`**

### ⚙️ Public Methods

#### 📁 AsyncFileSinkConfigurator.cs

- **`GetLogFiles`** → `IEnumerable<FileInfo>`
  - `IEnumerable<FileInfo> GetLogFiles()`

#### 📁 FExLoggingConfigurator.cs

- **`GetLogFiles`** → `IEnumerable<FileInfo>`
  - `IEnumerable<FileInfo> GetLogFiles()`

#### 📁 FExLoggingModule.cs

- **`CreateLogger`** → `ILogger`
  - Creating a `LoggerProviderCollection` lets Serilog optionally write events through other dynamically-added MEL ILoggerProviders.
  - `ILogger CreateLogger(LoggerProviderCollection providerCollection)`

- **`CreateLogger`** → `ILogger`
  - Creating a `LoggerProviderCollection` lets Serilog optionally write events through other dynamically-added MEL ILoggerProviders.
  - `ILogger CreateLogger(Type senderType)`

- **`GetLoggerProviderCollection`** → `LoggerProviderCollection`
  - Creating a `LoggerProviderCollection` lets Serilog optionally write events through other dynamically-added MEL ILoggerProviders.
  - `LoggerProviderCollection GetLoggerProviderCollection(ILoggerProvider[] loggerProviders)`

- **`GetSerilogLoggerFactory`** → `ILoggerFactory`
  - `ILoggerFactory GetSerilogLoggerFactory(LoggerProviderCollection providerCollection)`

#### 📁 FExLoggingService.cs

- **`GetLogger`** → `ILoggable`
  - `ILoggable GetLogger(object sender)`

#### 📁 LoggingConfiguration.cs

- **`HasOption`** → `bool`
  - `bool HasOption(LoggingOptions option)`

---

## FEx.Logging.Abstractions

**Namespace:** `Flakroup.FEx.LoggingAbstractions`  
**Classes:** 7 | **Interfaces:** 10 | **Enums:** 1
**Extension Methods:** 6 | **Methods:** 3 | **Properties:** 0

### 🔌 Extension Methods

#### Extensions for `ILoggable`

- **`Log`** → `void`
  - `void Log(this ILoggable loggable, LogLevel logLevel, string message, Exception exception = null)`
  - 📁 LoggerExtensions.cs

#### Extensions for `ILogger`

- **`Log`** → `void`
  - `void Log(this ILogger logger, LogLevel logLevel, string message, Exception exception = null)`
  - 📁 LoggerExtensions.cs

#### Extensions for `LoggerConfiguration`

- **`AddOverrides`** → `LoggerConfiguration`
  - `LoggerConfiguration AddOverrides(this LoggerConfiguration cfg,
                                                   IList<string> overrides,
                                                   LogEventLevel level)`
  - 📁 LoggerExtensions.cs

#### Extensions for `LoggingOptions`

- **`HasFlagFast`** → `bool`
  - `bool HasFlagFast(this LoggingOptions value, LoggingOptions flag)`
  - 📁 LoggingOptionsExtensions.cs

#### Extensions for `object`

- **`GetLogger`** → `ILoggable`
  - `ILoggable GetLogger(this object sender)`
  - 📁 LoggerExtensions.cs

- **`GetMicrosoftLogger`** → `ILogger`
  - `ILogger GetMicrosoftLogger(this object sender)`
  - 📁 LoggerExtensions.cs

### 🔷 Interfaces

- **`IFExLoggingConfigurator`**
- **`IFExLoggingContainer`**
- **`IFExLoggingService`**
- **`IFileSinkConfigurator`**
- **`ILoggable`**
- **`ILoggerState`**
- **`ILoggingConfiguration`**
- **`IPlatformLogger`**
- **`ISentryConfig`**
- **`ISinkConfigurator`**

### 📦 Classes

- **`FExLoggingStatics`**
- **`Loggable`**
- **`LoggerExtensions`**
- **`LoggerState`**
- **`LoggingOptionsExtensions`**
- **`SentrySinkConfiguratorBase`**
- **`SinkConfiguratorBase`**

### 🔢 Enums

- **`LoggingOptions`**

### ⚙️ Public Methods

#### 📁 FExLoggingStatics.cs

- **`Configure`** → `void`
  - Retrieves the instance. ⚠️ This is discouraged and should only be used where Dependency Injection is unavailable.
  - `void Configure(Func<ILoggerFactory> loggerFactoryFactory = null)`

- **`Initialize`** → `void`
  - Retrieves the instance. ⚠️ This is discouraged and should only be used where Dependency Injection is unavailable.
  - `void Initialize(ILoggerFactory loggerFactory, ILogger logger = null)`

- **`SetDefaults`** → `void`
  - Retrieves the instance. ⚠️ This is discouraged and should only be used where Dependency Injection is unavailable.
  - `void SetDefaults()`

---

## FEx.Maui

**Namespace:** `Flakroup.FEx.Maui`  
**Classes:** 1 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 1 | **Properties:** 0

### 📦 Classes

- **`FExMauiDispatcher`**

### ⚙️ Public Methods

#### 📁 FExMauiDispatcher.cs

- **`CheckAccess`** → `bool`
  - `bool CheckAccess(object sender = null)`

---

## FEx.MVVM

**Namespace:** `Flakroup.FEx.MVVM`  
**Classes:** 19 | **Interfaces:** 5 | **Enums:** 0
**Extension Methods:** 2 | **Methods:** 28 | **Properties:** 2

### 🔌 Extension Methods

#### Extensions for `IProgressAggregator`

- **`SetCurrentDownloadState`** → `void`
  - Sets the state of the current download.
  - `void SetCurrentDownloadState(this IProgressAggregator viewModel, DownloadProgressChangedEventArgs e)`
  - 📁 ProgressAggregatorExtensions.cs

- **`SetCurrentDownloadState`** → `void`
  - Sets the state of the current download.
  - `void SetCurrentDownloadState(this IProgressAggregator viewModel,
                                               double? bytesReceived,
                                               double? totalBytesToReceive = null,
                                               object userState = null)`
  - 📁 ProgressAggregatorExtensions.cs

### 🔷 Interfaces

- **`IAttachToContainerReceiver`**
- **`IFExMvvmContainer`**
- **`IProgressChange`**
- **`IProgressReceiver`** - Has progress status container instance
- **`IProgressService`**

### 📦 Classes

- **`BufferedProgressAggregator`**
- **`CheckedListItem`**
- **`FExMvvm`**
- **`FExMvvmModule`**
- **`FExTimer`**
- **`MessagePopupServiceBase`**
- **`NotifyProgressInfoChanged`**
- **`ProgressAggregator`**
- **`ProgressAggregatorExtensions`**
- **`ProgressChangesBuffer`**
- **`ProgressChangeSubject`**
- **`ProgressService`**
- **`ProgressSnapshot`**
- **`ProgressStatus`**
- **`PropertiesExtensions`**
- **`ReceiverDefinition`**
- **`SubscriberBase`**
- **`ViewField`**
- **`ViewModelBase`**

### ⚙️ Public Methods

#### 📁 BufferedProgressAggregator.cs

- **`PrgSet`** → `void`
  - Sets current progress value and maximal allowed value of the ProgressBar
  - `void PrgSet(double? value, double? maximum = null, ProgressChangeMode mode = ProgressChangeMode.Set)`

#### 📁 FExTimer.cs

- **`Start`** → `void`
  - Starts the timer.
  - `void Start()`

- **`Stop`** → `void`
  - Stops the timer.
  - `void Stop()`

- **`WithAsyncCallback`** → `IFExTimer`
  - Indicates whether this timer is running.
  - `IFExTimer WithAsyncCallback(Func<Task> asyncCallback, CancellationToken cancellationToken = default)`

- **`WithCallback`** → `IFExTimer`
  - Indicates whether this timer is running.
  - `IFExTimer WithCallback(Action callback)`

#### 📁 ProgressAggregator.cs (6 methods)

- **`GetProperties`** → `List<string>`
  - `List<string> GetProperties()`

- **`PrgAdd`** → `void`
  - Increments current progress value of the ProgressBar
  - `void PrgAdd(double addedValue = 1)`

- **`PrgMaxAdd`** → `void`
  - Adds value to the maximum of progress value.
  - `void PrgMaxAdd(double addedValue)`

- **`PrgSet`** → `void`
  - Sets current progress value and maximal allowed value of the ProgressBar
  - `void PrgSet(double? value, double? maximum = null, ProgressChangeMode mode = ProgressChangeMode.Set)`

- **`PrgSetEnd`** → `void`
  - Sets progress value of the ProgressBar to the maximal value
  - `void PrgSetEnd()`

- **`PrgSetMax`** → `void`
  - Sets maximal allowed value of the ProgressBar and resets current progress
  - `void PrgSetMax(double max)`

#### 📁 ProgressStatus.cs (14 methods)

- **`SetCurrItemInfo`** → `void`
  - `void SetCurrItemInfo(string value)`

- **`SetInfo`** → `void`
  - `void SetInfo(string value)`

- **`SetIsBusy`** → `void`
  - `void SetIsBusy(bool value)`

- **`SetIsFileOperation`** → `void`
  - `void SetIsFileOperation(bool value)`

- **`SetIsIndeterminate`** → `void`
  - `void SetIsIndeterminate(bool value)`

- **`SetIsInfoVisible`** → `void`
  - `void SetIsInfoVisible(bool? value)`

- **`SetMaximum`** → `void`
  - `void SetMaximum(double value)`

- **`SetMode`** → `void`
  - `void SetMode(ProgressOperationMode value)`

- **`SetPrecisePercentage`** → `void`
  - `void SetPrecisePercentage(double value)`

- **`SetState`** → `void`
  - `void SetState(ProgressState value)`

- **`SetStatusInfo`** → `void`
  - `void SetStatusInfo(string value)`

- **`SetThreadsInfo`** → `void`
  - `void SetThreadsInfo(string value)`

- **`SetUnit`** → `void`
  - `void SetUnit(string value)`

- **`SetValue`** → `void`
  - `void SetValue(double value)`

#### 📁 ReceiverDefinition.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

- **`IsReceivingThisProperty`** → `bool`
  - `bool IsReceivingThisProperty(string propertyName)`

#### 📁 ViewModelBase.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

### 📊 Properties

- **`Interval`** : `TimeSpan`
  - The timer interval
  - 📁 FExTimer.cs

- **`IsRunning`** : `bool`
  - Indicates whether this timer is running.
  - 📁 FExTimer.cs

---

## FEx.MVVM.Abstractions

**Namespace:** `Flakroup.FEx.MVVMAbstractions`  
**Classes:** 11 | **Interfaces:** 10 | **Enums:** 6
**Extension Methods:** 0 | **Methods:** 5 | **Properties:** 16

### 🔷 Interfaces

- **`IAsyncCommand`**
- **`IAsyncCommand`**
- **`IFExTimer`**
- **`ILink`**
- **`ILinkableNotifyPropertyChanged`**
- **`IMessagePopupService`**
- **`IProgressAggregator`**
- **`IProgressNotifyPropertyChanged`**
- **`IProgressStatus`**
- **`IViewModelBase`**

### 📦 Classes

- **`DialogOptionsBase`**
- **`DummyPopupService`**
- **`FExTreeViewNode`**
- **`FileDialogOptionsBase`**
- **`FolderBrowserDialogOptionsBase`**
- **`Link`**
- **`LinkableNotifyPropertyChanged`**
- **`OpenFileDialogOptionsBase`**
- **`ProgressPropertyChangedEventArgs`**
- **`SaveFileDialogOptionsBase`**
- **`WidthAndHeight`**

### 🔢 Enums

- **`FExMessageButton`** - Specifies the buttons that are displayed on a message box.
- **`MessageIcon`**
- **`MessageResult`** - Specifies which message box button that a user clicks.
- **`ProgressChangeMode`**
- **`ProgressOperationMode`**
- **`ProgressState`**

### ⚙️ Public Methods

#### 📁 FExTreeViewNode.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

- **`GetIconCacheKey`** → `string`
  - `string GetIconCacheKey()`

- **`GetNodePath`** → `List<string>`
  - `List<string> GetNodePath(string nodePath, char pathSeparator = '\\')`

#### 📁 Link.cs

- **`GetPropertyValue`** → `object`
  - `object GetPropertyValue()`

#### 📁 WidthAndHeight.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

### 📊 Properties

- **`CheckFileExists`** : `bool`
  - Gets or sets a value indicating whether [check file exists].
  - 📁 FileDialogOptionsBase.cs

- **`CheckPathExists`** : `bool`
  - Gets or sets a value indicating whether [check path exists].
  - 📁 FileDialogOptionsBase.cs

- **`DefaultExt`** : `string`
  - Gets or sets the default file name extension.
  - 📁 FileDialogOptionsBase.cs

- **`Description`** : `string`
  - Gets or sets the descriptive text displayed above the tree view control in the dialog box.
  - 📁 FolderBrowserDialogOptionsBase.cs

- **`FileName`** : `string`
  - Gets or sets a string containing the file name selected in the file dialog box.
  - 📁 FileDialogOptionsBase.cs

- **`FileNames`** : `string[]`
  - Gets an array that contains one file name for each selected file.
  - 📁 FileDialogOptionsBase.cs

- **`Filter`** : `string`
  - Gets or sets the current file name filter string, which determines the choices that appear in the "Save as file type" or "Files of type" box in the dialog box.
  - 📁 FileDialogOptionsBase.cs

- **`FilterIndex`** : `int`
  - Gets or sets the index of the filter currently selected in the file dialog box.
  - 📁 FileDialogOptionsBase.cs

- **`InitialDirectory`** : `string`
  - Gets or sets the initial directory displayed by the file dialog box.
  - 📁 FileDialogOptionsBase.cs

- **`Multiselect`** : `bool`
  - Gets or sets an option indicating whether allows users to select multiple files.
  - 📁 OpenFileDialogOptionsBase.cs

- **`PropertyName`** : `string`
  - Indicates the name of the property that changed.
  - 📁 ProgressPropertyChangedEventArgs.cs

- **`RestoreDirectory`** : `bool`
  - Gets or sets a value indicating whether the dialog box restores the directory to the previously selected directory before closing.
  - 📁 FileDialogOptionsBase.cs

- **`SelectedPath`** : `string`
  - Gets or sets the initial directory displayed by the file dialog box.
  - 📁 FolderBrowserDialogOptionsBase.cs

- **`ShowNewFolderButton`** : `bool`
  - Gets or sets the default file name extension.
  - 📁 FolderBrowserDialogOptionsBase.cs

- **`Title`** : `string`
  - Gets or sets the file dialog box title.
  - 📁 FileDialogOptionsBase.cs

- **`Value`** : `object`
  - Contains the value of the property that changed.
  - 📁 ProgressPropertyChangedEventArgs.cs

---

## FEx.MVVM.Rx

**Namespace:** `Flakroup.FEx.MVVMRx`  
**Classes:** 8 | **Interfaces:** 3 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 10 | **Properties:** 1

### 🔷 Interfaces

- **`IDisposableProgress`** - An that is disposable.
- **`IDisposableProgress`** - An that is disposable.
- **`IFExMvvmRxContainer`**

### 📦 Classes

- **`FExMvvmRx`**
- **`FExMvvmRxModule`**
- **`LinkableReactiveNotifyPropertyChanged`**
- **`ObservableProgress`** - A progress reporter that exposes progress updates as an observable stream. This is a hot observable.
- **`ReactiveNotifyPropertyChanged`**
- **`ReactiveObjectExtensions`**
- **`StatusHub`**
- **`StatusService`**

### ⚙️ Public Methods

#### 📁 ObservableProgress.cs

- **`CreateForUiWithBuffer`** → `IDisposableProgress<T>`
  - Creates a progress handler with common UI options: updates are buffered in intervals, and the is executed on the UI thread. This method must be called from the UI thread. The UI should already be initialized with the default state; is not invoked with an initial value.
  - `IDisposableProgress<T> CreateForUiWithBuffer(TimeSpan sampleTimeSpan,
                                                               Action<IList<T>> handler,
                                                               Func<IList<T>, bool> predicate = null,
                                                               bool limitToCurrentThread = false)`

- **`CreateForUiWithSample`** → `IDisposableProgress<T>`
  - Creates a progress handler with common UI options: updates are sampled on intervals, and the is executed on the UI thread. This method must be called from the UI thread. The UI should already be initialized with the default state; is not invoked with an initial value.
  - `IDisposableProgress<T> CreateForUiWithSample(TimeSpan sampleTimeSpan,
                                                               Action<T> handler,
                                                               IScheduler scheduler = null,
                                                               bool limitToCurrentThread = false)`

- **`CreateForUiWithSample`** → `IDisposableProgress<T>`
  - Creates a progress handler with common UI options: updates are sampled on 100ms intervals, and the is executed on the UI thread. This method must be called from the UI thread. The UI should already be initialized with the default state; is not invoked with an initial value.
  - `IDisposableProgress<T> CreateForUiWithSample(Action<T> handler, bool limitToCurrentThread = false)`

- **`CreateForUiWithSample`** → `IDisposableProgress<T>`
  - Creates a progress handler with common UI options: updates are sampled on 100ms intervals, and the is executed on the UI thread. This method must be called from the UI thread. The UI should already be initialized with the default state; is not invoked with an initial value.
  - `IDisposableProgress<T> CreateForUiWithSample(Action<T> handler,
                                                               IScheduler scheduler,
                                                               bool limitToCurrentThread = false)`

- **`Dispose`** → `void`
  - Creates a progress handler with common UI options: updates are sampled on 100ms intervals, and the is executed on the UI thread. This method must be called from the UI thread. The UI should already be initialized with the default state; is not invoked with an initial value.
  - `void Dispose()`

#### 📁 ReactiveNotifyPropertyChanged.cs

- **`OnPropertiesChanged`** → `void`
  - Use this method in your ReactiveObject classes when creating custom properties where raiseAndSetIfChanged doesn't suffice.
  - `void OnPropertiesChanged(params string[] propertyNames)`

- **`OnPropertyChanged`** → `void`
  - Use this method in your ReactiveObject classes when creating custom properties where raiseAndSetIfChanged doesn't suffice.
  - `void OnPropertyChanged([CallerMemberName] string propertyName = null)`

#### 📁 StatusHub.cs

- **`GetStatuses`** → `IList<string>`
  - `IList<string> GetStatuses()`

- **`GetStatusString`** → `string`
  - `string GetStatusString(string separator = null)`

#### 📁 StatusService.cs

- **`GetOrAdd`** → `IStatusHub`
  - `IStatusHub GetOrAdd(Guid? key = null,
                               Action<Guid, string> onStatusAdded = null,
                               Action<Guid, string> onStatusRemoved = null,
                               Action onStatusesReset = null,
                               bool markAsMain = false)`

### 📊 Properties

- **`IsDisposed`** : `bool`
  - A progress reporter that exposes progress updates as an observable stream. This is a hot observable.
  - 📁 ObservableProgress.cs

---

## FEx.NuGetx

**Namespace:** `Flakroup.FEx.NuGetx`  
**Classes:** 5 | **Interfaces:** 1 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 5 | **Properties:** 0

### 🔷 Interfaces

- **`INuGetExModule`**

### 📦 Classes

- **`NuGetEx`**
- **`NuGetExModule`**
- **`NuGetLogger`**
- **`NuGetManager`**
- **`NuGetPackageInstallation`**

### ⚙️ Public Methods

#### 📁 NuGetManager.cs

- **`GetIdentitiesAsync`** → `Task<PackageIdentity[]>`
  - `Task<PackageIdentity[]> GetIdentitiesAsync(
        PackageSearchMetadataBuilder.ClonedPackageSearchMetadata package)`

- **`GetNuGetOrgPackageMetadataResourceAsync`** → `Task<PackageMetadataResource>`
  - `Task<PackageMetadataResource> GetNuGetOrgPackageMetadataResourceAsync()`

- **`GetNuGetsToPublishAsync`** → `Task<FileInfo[]>`
  - `Task<FileInfo[]> GetNuGetsToPublishAsync(PackageMetadataResource packageMetadataResource,
                                                          FileInfo[] allNuGets,
                                                          HashSet<string> excludedPackageNames = null,
                                                          params string[] packagesToPublish)`

- **`GetNuGetsToPublishOnNuGetOrgAsync`** → `Task<FileInfo[]>`
  - `Task<FileInfo[]> GetNuGetsToPublishOnNuGetOrgAsync(FileInfo[] allNuGets,
                                                                    HashSet<string> excludedPackageNames = null,
                                                                    params string[] packagesToPublish)`

- **`GetPackageDependenciesAsync`** → `Task`
  - `Task GetPackageDependenciesAsync(PackageIdentity package,
                                                  NuGetFramework framework,
                                                  SourceCacheContext cacheContext,
                                                  INuGetLogger logger,
                                                  IEnumerable<SourceRepository> repositories,
                                                  ISet<SourcePackageDependencyInfo> availablePackages)`

---

## FEx.OneDrv

**Namespace:** `Flakroup.FEx.OneDrv`  
**Classes:** 1 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 0 | **Properties:** 0

### 📦 Classes

- **`OneDriveClient`**

---

## FEx.PersistentStorage

**Namespace:** `Flakroup.FEx.PersistentStorage`  
**Classes:** 8 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 6 | **Properties:** 0

### 📦 Classes

- **`CacheService`**
- **`DatabaseProvider`**
- **`FExPersistentStorageModule`**
- **`FileLocalStorageService`**
- **`LiteDbFileLocalStorageService`**
- **`LiteRepositoryExtensions`**
- **`LocalStorageService`**
- **`PlainFileLocalStorageService`**

### ⚙️ Public Methods

#### 📁 CacheService.cs

- **`GetCachedFile`** → `IFExCachedFile`
  - `IFExCachedFile GetCachedFile(Uri fileUrl)`

#### 📁 FileLocalStorageService.cs

- **`GetCachedFile`** → `IFExCachedFile`
  - `IFExCachedFile GetCachedFile(Uri fileUrl)`

#### 📁 LiteDbFileLocalStorageService.cs

- **`GetCachedFile`** → `IFExCachedFile`
  - `IFExCachedFile GetCachedFile(Uri fileUrl)`

#### 📁 LiteRepositoryExtensions.cs

- **`GetRepository`** → `LiteRepository`
  - `LiteRepository GetRepository(string dbFilePath)`

#### 📁 LocalStorageService.cs

- **`GetCachedFile`** → `IFExCachedFile`
  - `IFExCachedFile GetCachedFile(Uri fileUrl)`

#### 📁 PlainFileLocalStorageService.cs

- **`GetCachedFile`** → `IFExCachedFile`
  - `IFExCachedFile GetCachedFile(Uri fileUrl)`

---

## FEx.PersistentStorage.Abstractions

**Namespace:** `Flakroup.FEx.PersistentStorageAbstractions`  
**Classes:** 5 | **Interfaces:** 10 | **Enums:** 2
**Extension Methods:** 2 | **Methods:** 4 | **Properties:** 0

### 🔌 Extension Methods

#### Extensions for `ClearCacheReason`

- **`HasFlagFast`** → `bool`
  - `bool HasFlagFast(this ClearCacheReason value, ClearCacheReason flag)`
  - 📁 ClearCacheReasonExtensions.cs

#### Extensions for `LiteFileInfo<string>`

- **`IsExpired`** → `bool`
  - `bool IsExpired(this LiteFileInfo<string> file)`
  - 📁 CachedFileExtensions.cs

### 🔷 Interfaces

- **`ICacheableItem`**
- **`ICacheService`**
- **`IClearCache`**
- **`IDatabaseFilePathResolver`**
- **`IDatabaseProvider`**
- **`IFExCachedFile`**
- **`IFExDownloadResult`**
- **`IFExPersistentStorageContainer`**
- **`IFileLocalStorageService`**
- **`ILocalStorageService`**

### 📦 Classes

- **`CacheableItem`**
- **`CachedFileExtensions`**
- **`CacheServiceConfiguration`** *(static)*
- **`ClearCacheReasonExtensions`**
- **`FExCachedFile`**

### 🔢 Enums

- **`ClearCachePriority`**
- **`ClearCacheReason`**

### ⚙️ Public Methods

#### 📁 CachedFileExtensions.cs

- **`GetFileId`** → `string`
  - `string GetFileId(Uri fileUrl)`

- **`IsExpired`** → `bool`
  - `bool IsExpired(DateTime timestamp)`

#### 📁 FExCachedFile.cs

- **`GetDataStream`** → `MemoryStream`
  - `MemoryStream GetDataStream()`

- **`GetDataStreamAsync`** → `Task<MemoryStream>`
  - `Task<MemoryStream> GetDataStreamAsync(CancellationToken cancellationToken)`

---

## FEx.PersistentStorage.Rx

**Namespace:** `Flakroup.FEx.PersistentStorageRx`  
**Classes:** 6 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 0 | **Properties:** 0

### 📦 Classes

- **`AggregatedCollectionCachedSubject`**
- **`CachedSubjectBase`**
- **`CollectionCachedSubject`**
- **`CollectionCachedSubjectBase`**
- **`EnhancedCollectionCachedSubject`**
- **`SingleCachedSubject`**

---

## FEx.Platforms

**Namespace:** `Flakroup.FEx.Platforms`  
**Classes:** 4 | **Interfaces:** 2 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 15 | **Properties:** 0

### 🔷 Interfaces

- **`IFExPlatformsContainer`**
- **`IRegistryService`**

### 📦 Classes

- **`FExPlatforms`**
- **`FExPlatformsModule`**
- **`RegistryKeyExtensions`**
- **`RegistryService`**

### ⚙️ Public Methods

#### 📁 RegistryService.cs (15 methods)

- **`Get45PlusFromRegistry`** → `List<Version>`
  - `List<Version> Get45PlusFromRegistry()`

- **`GetClassesRootSubKey`** → `RegistryKey`
  - `RegistryKey GetClassesRootSubKey(string subKey, bool writable = true)`

- **`GetCurrentUserSubKey`** → `RegistryKey`
  - `RegistryKey GetCurrentUserSubKey(string subKey, bool writable = true)`

- **`GetDefaultExtension`** → `string`
  - `string GetDefaultExtension(string mimeType)`

- **`GetDefaultExtension`** → `string`
  - `string GetDefaultExtension(MediaTypes mediaType)`

- **`GetDefaultMimeType`** → `string`
  - `string GetDefaultMimeType(string extension)`

- **`GetInstalledApplications`** → `List<RegistryKey>`
  - `List<RegistryKey> GetInstalledApplications()`

- **`GetLocalMachineSubKey`** → `RegistryKey`
  - `RegistryKey GetLocalMachineSubKey(string subKey, bool writable = true)`

- **`GetOrAddCurrentUserSubKey`** → `RegistryKey`
  - `RegistryKey GetOrAddCurrentUserSubKey(string subKey, bool writable = true)`

- **`GetOrAddLocalMachineSubKey`** → `RegistryKey`
  - `RegistryKey GetOrAddLocalMachineSubKey(string subKey, bool writable = true)`

- **`GetOrAddRegistryKeyStringValue`** → `string`
  - `string GetOrAddRegistryKeyStringValue(string path, string keyName, Func<string> getNewValue)`

- **`GetStandardBrowserPath`** → `string`
  - `string GetStandardBrowserPath()`

- **`GetSubKey`** → `RegistryKey`
  - `RegistryKey GetSubKey(RegistryKey registry, string subKey, bool writable = true)`

- **`GetVersionFromRegistry`** → `List<Version>`
  - `List<Version> GetVersionFromRegistry()`

- **`SetStartup`** → `void`
  - `void SetStartup(string appName, string executablePath, bool enable, bool global = false)`

---

## FEx.Platforms.Windows

**Namespace:** `Flakroup.FEx.PlatformsWindows`  
**Classes:** 4 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 4 | **Methods:** 0 | **Properties:** 0

### 🔌 Extension Methods

#### Extensions for `DirectoryInfo`

- **`SetEverybodyFullControl`** → `bool`
  - `bool SetEverybodyFullControl(this DirectoryInfo dInfo, params string[] excludes)`
  - 📁 FileSystemExtensions.cs

#### Extensions for `IIdentity`

- **`Domain`** → `string`
  - Extracts the domain name part of the full identity name
  - `string Domain(this IIdentity user)`
  - 📁 IdentityExtensions.cs

- **`LogFormattedName`** → `string`
  - Extracts the SAMAccountName part of the full identity name and formats it the way the activity log wants it.
  - `string LogFormattedName(this IIdentity user)`
  - 📁 IdentityExtensions.cs

- **`SamAccountName`** → `string`
  - Extracts the SAMAccountName part of the full identity name
  - `string SamAccountName(this IIdentity user)`
  - 📁 IdentityExtensions.cs

### 📦 Classes

- **`ACL`**
- **`ElevatedCmd`**
- **`FileSystemExtensions`**
- **`IdentityExtensions`** - Extension to extract various parts of the identity name.

---

## FEx.RESXx

**Namespace:** `Flakroup.FEx.RESXx`  
**Classes:** 1 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 2 | **Properties:** 0

### 📦 Classes

- **`ResxManager`**

### ⚙️ Public Methods

#### 📁 ResxManager.cs

- **`GetResourcesEntries`** → `Dictionary<string, string>`
  - `Dictionary<string, string> GetResourcesEntries(string path)`

- **`Resgen`** → `string[]`
  - Resgens the specified file.
  - `string[] Resgen(string resx, string resxDesigner, string fileName, string projectNamespace)`

---

## FEx.SecureStorage

**Namespace:** `Flakroup.FEx.SecureStorage`  
**Classes:** 1 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 1 | **Properties:** 0

### 📦 Classes

- **`SecureStorageService`**

### ⚙️ Public Methods

#### 📁 SecureStorageService.cs

- **`Set`** → `void`
  - `void Set(string key, object content)`

---

## FEx.Telemetry

**Namespace:** `Flakroup.FEx.Telemetry`  
**Classes:** 3 | **Interfaces:** 2 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 0 | **Properties:** 0

### 🔷 Interfaces

- **`IFExTelemetryConfig`**
- **`IFExTelemetryModule`**

### 📦 Classes

- **`FExTelemetryConfigBase`**
- **`FExTelemetryModule`**
- **`TelemetryAccessTokenSubject`**

---

## FEx.Telemetry.Rollbar

**Namespace:** `Flakroup.FEx.TelemetryRollbar`  
**Classes:** 4 | **Interfaces:** 3 | **Enums:** 0
**Extension Methods:** 0 | **Methods:** 0 | **Properties:** 0

### 🔷 Interfaces

- **`IRollbarConfig`**
- **`IRollbarModule`**
- **`IRollbarService`**

### 📦 Classes

- **`FExRollbarModule`**
- **`FExRollbarx`**
- **`RollbarConfigBase`**
- **`RollbarService`**

---

## FEx.WebScraping

**Namespace:** `Flakroup.FEx.WebScraping`  
**Classes:** 5 | **Interfaces:** 2 | **Enums:** 0
**Extension Methods:** 11 | **Methods:** 0 | **Properties:** 0

### 🔌 Extension Methods

#### Extensions for `HtmlDocument`

- **`SaveToFile`** → `string`
  - `string SaveToFile(this HtmlDocument doc, string path = null)`
  - 📁 HtmlDocumentExtensions.cs

#### Extensions for `HtmlNode`

- **`GetHref`** → `string`
  - Helper method to get the value of an attribute of this node. If the attribute is not found, the default value will be returned.
  - `string GetHref(this HtmlNode value)`
  - 📁 HtmlNodeExtensions.cs

- **`GetNodeAttributeBoolValue`** → `bool`
  - Helper method to get the value of an attribute of this node. If the attribute is not found, the default value will be returned.
  - `bool GetNodeAttributeBoolValue(this HtmlNode value, string name, bool def = false)`
  - 📁 HtmlNodeExtensions.cs

- **`GetNodeAttributeIntValue`** → `int`
  - Helper method to get the value of an attribute of this node. If the attribute is not found, the default value will be returned.
  - `int GetNodeAttributeIntValue(this HtmlNode value, string name, int def = 0)`
  - 📁 HtmlNodeExtensions.cs

- **`GetNodeAttributeStringValue`** → `string`
  - Helper method to get the value of an attribute of this node. If the attribute is not found, the default value will be returned.
  - `string GetNodeAttributeStringValue(this HtmlNode value, string name, string def = null)`
  - 📁 HtmlNodeExtensions.cs

- **`GetNodeTypeAttributeValue`** → `string`
  - Helper method to get the value of an attribute of this node. If the attribute is not found, the default value will be returned.
  - `string GetNodeTypeAttributeValue(this HtmlNode value)`
  - 📁 HtmlNodeExtensions.cs

- **`GetSrc`** → `string`
  - Helper method to get the value of an attribute of this node. If the attribute is not found, the default value will be returned.
  - `string GetSrc(this HtmlNode value)`
  - 📁 HtmlNodeExtensions.cs

- **`IsInBody`** → `bool`
  - Helper method to get the value of an attribute of this node. If the attribute is not found, the default value will be returned.
  - `bool IsInBody(this HtmlNode value)`
  - 📁 HtmlNodeExtensions.cs

- **`IsNodeLinkElementOfType`** → `bool`
  - Helper method to get the value of an attribute of this node. If the attribute is not found, the default value will be returned.
  - `bool IsNodeLinkElementOfType(this HtmlNode value, string type)`
  - 📁 HtmlNodeExtensions.cs

- **`SelectNodes`** → `HtmlNodeCollection`
  - Selects a list of nodes matching the expression.
  - `HtmlNodeCollection SelectNodes(this HtmlNode value, Func<XPathBuilderEx, XPathBuilderEx> xpath)`
  - 📁 HtmlNodeExtensions.cs

- **`SelectSingleNode`** → `HtmlNode`
  - Selects the first XmlNode that matches the XPath expression.
  - `HtmlNode SelectSingleNode(this HtmlNode value, Func<XPathBuilderEx, XPathBuilderEx> xpath)`
  - 📁 HtmlNodeExtensions.cs

### 🔷 Interfaces

- **`IWebBrowserScraper`**
- **`IWebScraper`**

### 📦 Classes

- **`HtmlDocumentExtensions`**
- **`HtmlNodeExtensions`**
- **`HtmlWebHelper`**
- **`XPathBuilderBase`**
- **`XPathBuilderEx`**

---

## FEx.Webx

**Namespace:** `Flakroup.FEx.Webx`  
**Classes:** 8 | **Interfaces:** 0 | **Enums:** 0
**Extension Methods:** 6 | **Methods:** 6 | **Properties:** 2

### 🔌 Extension Methods

#### Extensions for `HttpResponseMessage`

- **`GetDefaultExtension`** → `string`
  - `string GetDefaultExtension(this HttpResponseMessage response)`
  - 📁 HttpResponseExtensions.cs

- **`GetFileName`** → `string`
  - `string GetFileName(this HttpResponseMessage response)`
  - 📁 UriExtensions.cs

#### Extensions for `HttpWebResponse`

- **`GetDefaultExtension`** → `string`
  - `string GetDefaultExtension(this HttpWebResponse response)`
  - 📁 HttpResponseExtensions.cs

- **`GetFileName`** → `string`
  - `string GetFileName(this HttpWebResponse response)`
  - 📁 UriExtensions.cs

#### Extensions for `MediaTypeHeaderValue`

- **`GetDefaultExtension`** → `string`
  - `string GetDefaultExtension(this MediaTypeHeaderValue contentType)`
  - 📁 MimeTypesUtility.cs

- **`GetDefaultExtensions`** → `IReadOnlyCollection<string>`
  - `IReadOnlyCollection<string> GetDefaultExtensions(this MediaTypeHeaderValue contentType)`
  - 📁 MimeTypesUtility.cs

### 📦 Classes

- **`FExWebx`**
- **`HasInternetConnectionGate`**
- **`HttpResponseExtensions`**
- **`JsonExtensions`**
- **`MimeTypesUtility`**
- **`NetworkUtilities`** - Network-related utilities.
- **`ResponseResult`**
- **`UriExtensions`**

### ⚙️ Public Methods

#### 📁 HasInternetConnectionGate.cs

- **`CheckAsync`** → `Task<bool>`
  - `Task<bool> CheckAsync(Uri url = null)`

#### 📁 MimeTypesUtility.cs

- **`GetDefaultExtension`** → `string`
  - `string GetDefaultExtension(string contentType, string fileName)`

- **`GetDefaultExtensions`** → `IReadOnlyCollection<string>`
  - `IReadOnlyCollection<string> GetDefaultExtensions(MediaTypes mediaType)`

- **`GetDefaultExtensions`** → `IReadOnlyCollection<string>`
  - `IReadOnlyCollection<string> GetDefaultExtensions(string mimeType)`

- **`GetDefaultMimeType`** → `string`
  - `string GetDefaultMimeType(string extension)`

#### 📁 NetworkUtilities.cs

- **`GetCredentials`** → `ICredentials`
  - Creates network credentials from username and password, or returns default credentials if not provided.
  - `ICredentials GetCredentials(string username = "", string password = "")`

### 📊 Properties

- **`IsSuccess`** : `bool`
  - Gets a value indicating whether this instance is success.
  - 📁 ResponseResult.cs

- **`ResponseBody`** : `string`
  - Gets the response body.
  - 📁 ResponseResult.cs

---

## FEx.WPFx

**Description:**   

**Namespace:** `Flakroup.FEx.WPFx`  
**Classes:** 70 | **Interfaces:** 7 | **Enums:** 5
**Extension Methods:** 17 | **Methods:** 111 | **Properties:** 46

### 🔌 Extension Methods

#### Extensions for `Bitmap`

- **`ToBitmapSource`** → `BitmapSource`
  - To the bitmap source.
  - `BitmapSource ToBitmapSource(this Bitmap bitmap)`
  - 📁 BitmapExtensions.cs

#### Extensions for `BitmapSource`

- **`SaveToFile`** → `void`
  - Saves to file.
  - `void SaveToFile(this BitmapSource image, string filePath)`
  - 📁 BitmapExtensions.cs

#### Extensions for `DispatcherObject`

- **`GetDispatcherObject`** → `DispatcherObject`
  - Shows the view and waits until it's closed.
  - `DispatcherObject GetDispatcherObject(this DispatcherObject sender)`
  - 📁 DispatcherService.cs

#### Extensions for `DrawingImage`

- **`ToMemoryStream`** → `MemoryStream`
  - Converts the SVG to image source.
  - `MemoryStream ToMemoryStream(this DrawingImage drawingImage)`
  - 📁 SvgCommon.cs

- **`ToRenderTargetBitmap`** → `RenderTargetBitmap`
  - Converts the SVG to image source.
  - `RenderTargetBitmap ToRenderTargetBitmap(this DrawingImage source)`
  - 📁 SvgCommon.cs

#### Extensions for `IntPtr`

- **`MaximizeWindow`** → `void`
  - `void MaximizeWindow(this IntPtr hwnd)`
  - 📁 NativeMethods.cs

- **`MinimizeWindow`** → `void`
  - `void MinimizeWindow(this IntPtr hwnd)`
  - 📁 NativeMethods.cs

#### Extensions for `Process`

- **`MaximizeWindow`** → `void`
  - `void MaximizeWindow(this Process proc)`
  - 📁 NativeMethods.cs

- **`MinimizeWindow`** → `void`
  - `void MinimizeWindow(this Process proc)`
  - 📁 NativeMethods.cs

#### Extensions for `Stream`

- **`ToBitmapImage`** → `BitmapImage`
  - `BitmapImage ToBitmapImage(this Stream stream,
                                            bool forceLoad = false,
                                            int decodePixelHeight = 0,
                                            int decodePixelWidth = 0)`
  - 📁 BitmapExtensions.cs

#### Extensions for `Visual`

- **`GetDpiFactor`** → `Point`
  - Centers the window on the screen.
  - `Point GetDpiFactor(this Visual control)`
  - 📁 WindowsExtensions.cs

#### Extensions for `Window`

- **`BringOnTop`** → `void`
  - Brings window to the foreground.
  - `void BringOnTop(this Window window)`
  - 📁 NativeMethods.cs

- **`CenterWindowOnTheScreen`** → `void`
  - Centers the window on the screen.
  - `void CenterWindowOnTheScreen(this Window window, Screen screen)`
  - 📁 WindowsExtensions.cs

- **`CenterWindowOnTopOfTheOwner`** → `void`
  - Centers the window on top of the owner.
  - `void CenterWindowOnTopOfTheOwner(this Window window)`
  - 📁 WindowsExtensions.cs

- **`GetWindowsScreen`** → `Screen`
  - Gets the screen on which window is present.
  - `Screen GetWindowsScreen(this Window window)`
  - 📁 WindowsExtensions.cs

- **`PlaceToMonitor`** → `void`
  - Places to primary monitor.
  - `void PlaceToMonitor(this Window window, Screen screen)`
  - 📁 WindowsExtensions.cs

- **`PlaceToPrimaryMonitor`** → `void`
  - Places to primary monitor.
  - `void PlaceToPrimaryMonitor(this Window window)`
  - 📁 WindowsExtensions.cs

### 🔷 Interfaces

- **`IAppConfig`**
- **`IFExContainer`**
- **`IFExWpfxContainer`**
- **`IItemSizeProvider`** - Provides the size of items displayed in an VirtualizingPanel.
- **`ITreeViewBuilder`**
- **`ITreeViewBuilder`**
- **`IViewDesign`**

### 📦 Classes

- **`AdditionConverter`**
- **`AppBootstrapper`**
- **`AppConfig`**
- **`BaseMappingConverter`** - Base class for mapping converters.
- **`BindingErrorListener`** - Raises an event each time a WPF Binding error occurs.
- **`BindingException`** - Exception thrown by the BindingExceptionThrower each time a WPF binding error occurs
- **`BindingExceptionThrower`** - Converts WPF binding error into BindingException
- **`BitmapExtensions`**
- **`BoolAndConverter`**
- **`BoolAndToVisibilityConverter`**
- **`BoolInvertedConverter`** - Bool invert converter.
- **`BoolToVisibilityConverter`** - Bool to visibility converter.
- **`BoolToVisibilityInvertedConverter`** - Bool to Visibility Invert Converter.
- **`ByteToSizeConverter`**
- **`CmdLineHandler`** *(static)*
- **`CmdLineTarget`**
- **`CollectionIsNotNullOrEmptyToBoolConverter`** - Represents the converter that converts the inverse of a Boolean values to and from System.Windows.Visibility enumeration values.
- **`CollectionIsNotNullOrEmptyToVisibilityConverter`** - Represents the converter that converts the inverse of a Boolean values to and from System.Windows.Visibility enumeration values.
- **`CollectionIsNullOrEmptyToBoolConverter`** - Represents the converter that converts the inverse of a Boolean values to and from System.Windows.Visibility enumeration values.
- **`CommonWindowsImaging`**
- **`ConvertedSvgData`**
- **`ConverterLogic`**
- **`DependencyPropertyExtensions`**
- **`DispatcherContextExecutor`**
- **`DispatcherService`**
- **`DivisionConverter`**
- **`EnumToStringConverter`**
- **`EnumToVisibilityConverter`** - Enum to visibility converter.
- **`FExModule`**
- **`FExWpfx`**
- **`FExWpfxModule`**
- **`FileSystemIconsProvider`**
- **`FileUtil`**
- **`FileUtils`**
- **`FolderBrowserDialogOptions`**
- **`GridView`** - Simple control that displays a gird of items. Depending on the orientation, the items are either stacked horizontally or vertically until the items are wrapped to the next row or column. The control is using virtualization to support large amount of items. In order to work properly all items must have the same size.
- **`ItemsToFirstItemConverter`** - Items to first item converter.
- **`LongToTimeStringConverter`**
- **`Monitorinfo`**
- **`MultiplicationConverter`**
- **`NativeImagingMethods`** - Contains the external references to the unmanaged code.
- **`NativeMethods`** - Contains the external references to the unmanaged code.
- **`NotEmptyValidationRule`**
- **`NullableBoolInvertedConverter`**
- **`NullToVisibilityConverter`**
- **`NullToVisibilityInvertedConverter`**
- **`OpenFileDialogOptions`**
- **`OutlinedTextBlock`**
- **`ResizeModeToVisibilityConverter`**
- **`ResKeyInfo`**
- **`ResourceIdHelper`** *(static)*
- **`SaveFileDialogOptions`**
- **`ShellIcon`** - Get a small or large Icon with an easy C# function call that returns a 32x32 or 16x16 System.Drawing.Icon depending on which function you call either GetSmallIcon(string fileName) or GetLargeIcon(string fileName)
- **`Splash`**
- **`StringToBooleanConverter`** - String to visibility converter.
- **`StringToVisibilityConverter`** - String to visibility converter.
- **`SubtractionConverter`**
- **`SvgCommon`**
- **`TreeViewBuilder`**
- **`TreeViewBuilderBase`**
- **`ViewDesign`**
- **`VirtualizingItemsControl`** - A ItemsControl supporting virtualization.
- **`VirtualizingPanelBase`** - Base class for panels which are supporting virtualization.
- **`VirtualizingPanelBaseV1`** - Base class for panels which are supporting virtualization.
- **`VirtualizingWrapPanel`** - A implementation of a wrap panel that supports virtualization and can be used in horizontal and vertical orientation. https://github.com/sbaeumlisberger/VirtualizingWrapPanel
- **`VirtualizingWrapPanelV1`** - A implementation of a wrap panel that supports virtualization and can be used in horizontal and vertical orientation. In order to work properly all items must have the same size.
- **`VirtualizingWrapPanelWithItemExpansion`** - A implementation of a wrap panel that supports virtualization and can be used in horizontal and vertical orientation. In addition the panel allows to expand one specific item. In order to work properly all items must have the same size.
- **`Win32`**
- **`WindowsExtensions`**
- **`WpfMessagePopupService`**

### 🔢 Enums

- **`PathIs`**
- **`ResultMode`**
- **`ScrollDirection`**
- **`SHGFI`**
- **`SpacingMode`** - Specifies how remaining space is distributed.

### ⚙️ Public Methods

#### 📁 AdditionConverter.cs

- **`Convert`** → `object`
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 BaseMappingConverter.cs

- **`Convert`** → `object`
  - Converts a value.
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - Converts a value.
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 BindingErrorListener.cs

- **`Dispose`** → `void`
  - Event raised each time a WPF binding error occurs
  - `void Dispose()`

#### 📁 BindingException.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

#### 📁 BindingExceptionThrower.cs

- **`Attach`** → `void`
  - Start listening WPF binding error
  - `void Attach(string bindingErrorsCacheDirectory)`

- **`Detach`** → `void`
  - Stop listening WPF binding error
  - `void Detach()`

#### 📁 BitmapExtensions.cs

- **`GetSize`** → `WidthAndHeight`
  - Takes a bitmap and converts it to an image that can be handled by WPF ImageBrush
  - `WidthAndHeight GetSize(Stream originalStream)`

#### 📁 BoolAndConverter.cs

- **`Convert`** → `object`
  - `object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object[]`
  - `object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)`

#### 📁 BoolAndToVisibilityConverter.cs

- **`Convert`** → `object`
  - `object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object[]`
  - `object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)`

#### 📁 BoolInvertedConverter.cs

- **`Convert`** → `object`
  - Converts a value.
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - Converts a value.
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 BoolToVisibilityInvertedConverter.cs

- **`Convert`** → `object`
  - Converts a value.
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 ByteToSizeConverter.cs

- **`Convert`** → `object`
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 CmdLineHandler.cs

- **`HandleCommandLine`** → `int`
  - `int HandleCommandLine(string arg)`

- **`HandleCommandLine`** → `int`
  - `int HandleCommandLine(string[] args)`

#### 📁 CmdLineTarget.cs

- **`BuildDict`** → `int`
  - `int BuildDict(
        //[ArgumentParam(Aliases = "i", Desc = "dir to the SVGs", LongDesc = "specify folder of the graphic files to process")`

#### 📁 CollectionIsNotNullOrEmptyToBoolConverter.cs

- **`Convert`** → `object`
  - Converts a Boolean value to a System.Windows.Visibility enumeration value.
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - Converts a System.Windows.Visibility enumeration value to a Boolean value.
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 CollectionIsNotNullOrEmptyToVisibilityConverter.cs

- **`Convert`** → `object`
  - Converts a Boolean value to a System.Windows.Visibility enumeration value.
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - Converts a System.Windows.Visibility enumeration value to a Boolean value.
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 CollectionIsNullOrEmptyToBoolConverter.cs

- **`Convert`** → `object`
  - Converts a Boolean value to a System.Windows.Visibility enumeration value.
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - Converts a System.Windows.Visibility enumeration value to a Boolean value.
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 CommonWindowsImaging.cs

- **`ConvertStreamToBitmapImageAsync`** → `Task<BitmapImage>`
  - Converts the byte array to bitmap image.
  - `Task<BitmapImage> ConvertStreamToBitmapImageAsync(Stream imageStream)`

- **`GetBitmapImageFromFileAsync`** → `Task<BitmapImage>`
  - Converts the byte array to bitmap image.
  - `Task<BitmapImage> GetBitmapImageFromFileAsync(string filePath,
                                                                      WidthAndHeight size = null,
                                                                      bool forceLoad = false,
                                                                      bool forceMemoryStream = true,
                                                                      bool lockOnFile = true)`

- **`GetBitmapImageFromFileAsync`** → `Task<BitmapImage>`
  - Converts the byte array to bitmap image.
  - `Task<BitmapImage> GetBitmapImageFromFileAsync(FileInfo file,
                                                                      WidthAndHeight size = null,
                                                                      bool forceLoad = false,
                                                                      bool forceMemoryStream = true,
                                                                      bool lockOnFile = true)`

#### 📁 ConverterLogic.cs

- **`ConvertSvg`** → `ConvertedSvgData`
  - `ConvertedSvgData ConvertSvg(string filepath)`

- **`ConvertSvg`** → `ConvertedSvgData`
  - `ConvertedSvgData ConvertSvg(string svg, string fileName)`

- **`ConvertSvgToObject`** → `object`
  - `object ConvertSvgToObject(ConvertedSvgData svg,
                                            ResultMode resultMode,
                                            WpfDrawingSettings wpfDrawingSettings,
                                            out string name,
                                            ResKeyInfo resKeyInfo)`

- **`GetPathGeometries`** → `IEnumerable<PathGeometry>`
  - `IEnumerable<PathGeometry> GetPathGeometries(Drawing drawing)`

- **`ReplaceBrushesInDrawingGroupsOld`** → `void`
  - This one uses local and global colors
  - `void ReplaceBrushesInDrawingGroupsOld(XElement rootElement, ResKeyInfo resKeyInfo)`

#### 📁 DispatcherContextExecutor.cs

- **`CheckAccess`** → `bool`
  - `bool CheckAccess(object sender = null)`

#### 📁 DispatcherService.cs

- **`BeginInvoke`** → `void`
  - Checks the access.
  - `void BeginInvoke(Action action,
                                   DispatcherObject sender = null,
                                   DispatcherPriority priority = DispatcherPriority.Normal)`

- **`CheckAccess`** → `bool`
  - Checks the access.
  - `bool CheckAccess(DispatcherObject sender)`

- **`ExecuteTaskInDispatcherContextAsync`** → `Task`
  - Executes the action in dispatcher context by checking if action should be invoked by dispatcher, or directly, and running it.
  - `Task ExecuteTaskInDispatcherContextAsync(Func<Task> funcTask,
                                                                 DispatcherObject sender = null,
                                                                 DispatcherPriority priority = DispatcherPriority.Send)`

- **`InvokeOnDispatcherContext`** → `void`
  - Executes the action in dispatcher context by checking if action should be invoked by dispatcher asynchronously, or directly, and running it.
  - `void InvokeOnDispatcherContext(Action action,
                                                 DispatcherObject sender = null,
                                                 DispatcherPriority priority = DispatcherPriority.Send)`

- **`InvokeOnDispatcherContextAsync`** → `Task`
  - Executes the action in dispatcher context by checking if action should be invoked by dispatcher asynchronously, or directly, and running it.
  - `Task InvokeOnDispatcherContextAsync(Action action,
                                                            DispatcherObject sender = null,
                                                            DispatcherPriority priority = DispatcherPriority.Send)`

#### 📁 DivisionConverter.cs

- **`Convert`** → `object`
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 EnumToStringConverter.cs

- **`Convert`** → `object`
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 EnumToVisibilityConverter.cs

- **`Convert`** → `object`
  - Converts a value.
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - Not implemented
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 FExWPFx.cs

- **`OverrideFormattingOnUI`** → `void`
  - Overrides formatting on UI.
  - `void OverrideFormattingOnUI(CultureInfo culture = null)`

#### 📁 FileSystemIconsProvider.cs

- **`GetFileIconAsync`** → `Task<BitmapSource>`
  - `Task<BitmapSource> GetFileIconAsync(string filePath, bool isIconAttachedToFile = true)`

#### 📁 FileUtil.cs

- **`WhoIsLocking`** → `List<Process>`
  - Find out what process(es) have a lock on the specified file.
  - `List<Process> WhoIsLocking(string path)`

#### 📁 FileUtils.cs

- **`MakeRelativePath`** → `string`
  - Creates a relative path from one file or folder to another.
  - `string MakeRelativePath(string fromPath, PathIs fromIs, string toPath, PathIs toIs)`

#### 📁 FolderBrowserDialogOptions.cs

- **`ShowDialog`** → `DialogResult`
  - Shows the dialog.
  - `DialogResult ShowDialog(Window owner = null, IProgressAggregator viewModel = null)`

- **`ShowDialogOk`** → `bool`
  - Shows the dialog.
  - `bool ShowDialogOk(Window owner = null, IProgressAggregator viewModel = null)`

#### 📁 ItemContainerInfo.cs

- **`GetHashCode`** → `int`
  - `int GetHashCode()`

#### 📁 ItemsToFirstItemConverter.cs

- **`Convert`** → `object`
  - Converts a value.
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - Converts a value.
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 LongToTimeStringConverter.cs

- **`Convert`** → `object`
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 MultiplicationConverter.cs

- **`Convert`** → `object`
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 NativeImagingMethods.cs

- **`DeleteBitmapObject`** → `void`
  - Contains the external references to the unmanaged code.
  - `void DeleteBitmapObject(IntPtr handle)`

#### 📁 NativeMethods.cs

- **`GetLastError`** → `Win32Exception`
  - Gets the last error.
  - `Win32Exception GetLastError()`

- **`WmGetMinMaxInfo`** → `void`
  - Gets the root windows of process.
  - `void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)`

#### 📁 NotEmptyValidationRule.cs

- **`Validate`** → `ValidationResult`
  - `ValidationResult Validate(object value, CultureInfo cultureInfo)`

#### 📁 NullableBoolInvertedConverter.cs

- **`Convert`** → `object`
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 NullToVisibilityConverter.cs

- **`Convert`** → `object`
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 NullToVisibilityInvertedConverter.cs

- **`Convert`** → `object`
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 ObservableTraceListener.cs

- **`TraceEvent`** → `void`
  - A TraceListener that raise an event each time a trace is written
  - `void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)`

- **`Write`** → `void`
  - A TraceListener that raise an event each time a trace is written
  - `void Write(string message)`

- **`WriteLine`** → `void`
  - A TraceListener that raise an event each time a trace is written
  - `void WriteLine(string message)`

#### 📁 OpenFileDialogOptions.cs

- **`ShowDialog`** → `bool`
  - Shows the dialog.
  - `bool ShowDialog(Window owner = null, IProgressAggregator viewModel = null)`

#### 📁 RectStruct.cs

- **`Equals`** → `bool`
  - Determine if 2 RECT are equal (deep compare)
  - `bool Equals(object obj)`

- **`GetHashCode`** → `int`
  - Return the HashCode for this struct (not garanteed to be unique)
  - `int GetHashCode()`

- **`ToString`** → `string`
  - Return a user friendly representation of this struct
  - `string ToString()`

#### 📁 ResizeModeToVisibilityConverter.cs

- **`Convert`** → `object`
  - Converts a value.
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - Converts a value.
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 ResourceIdHelper.cs

- **`GetResourceIdFromRelativePath`** → `string`
  - `string GetResourceIdFromRelativePath(string relPath)`

#### 📁 SaveFileDialogOptions.cs

- **`ShowDialog`** → `bool`
  - Shows the dialog.
  - `bool ShowDialog(Window owner = null, IProgressAggregator viewModel = null)`

#### 📁 ShellIcon.cs (6 methods)

- **`GetLargeFolderIcon`** → `Icon`
  - Get a small or large Icon with an easy C# function call that returns a 32x32 or 16x16 System.Drawing.Icon depending on which function you call either GetSmallIcon(string fileName) or GetLargeIcon(string fileName)
  - `Icon GetLargeFolderIcon()`

- **`GetLargeIcon`** → `Icon`
  - Get a small or large Icon with an easy C# function call that returns a 32x32 or 16x16 System.Drawing.Icon depending on which function you call either GetSmallIcon(string fileName) or GetLargeIcon(string fileName)
  - `Icon GetLargeIcon(string fileName)`

- **`GetLargeIconFromExtension`** → `Icon`
  - Get a small or large Icon with an easy C# function call that returns a 32x32 or 16x16 System.Drawing.Icon depending on which function you call either GetSmallIcon(string fileName) or GetLargeIcon(string fileName)
  - `Icon GetLargeIconFromExtension(string extension)`

- **`GetSmallFolderIcon`** → `Icon`
  - Get a small or large Icon with an easy C# function call that returns a 32x32 or 16x16 System.Drawing.Icon depending on which function you call either GetSmallIcon(string fileName) or GetLargeIcon(string fileName)
  - `Icon GetSmallFolderIcon()`

- **`GetSmallIcon`** → `Icon`
  - Get a small or large Icon with an easy C# function call that returns a 32x32 or 16x16 System.Drawing.Icon depending on which function you call either GetSmallIcon(string fileName) or GetLargeIcon(string fileName)
  - `Icon GetSmallIcon(string fileName)`

- **`GetSmallIconFromExtension`** → `Icon`
  - Get a small or large Icon with an easy C# function call that returns a 32x32 or 16x16 System.Drawing.Icon depending on which function you call either GetSmallIcon(string fileName) or GetLargeIcon(string fileName)
  - `Icon GetSmallIconFromExtension(string extension)`

#### 📁 StringToBooleanConverter.cs

- **`Convert`** → `object`
  - Converts a value.
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - Converts a value.
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 StringToVisibilityConverter.cs

- **`Convert`** → `object`
  - Converts a value.
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - Converts a value.
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 SubstractionConverter.cs

- **`Convert`** → `object`
  - `object Convert(object value, Type targetType, object parameter, CultureInfo culture)`

- **`ConvertBack`** → `object`
  - `object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)`

#### 📁 SvgCommon.cs

- **`ConvertSvgFileToDrawingImage`** → `DrawingImage`
  - Converts the SVG file to .
  - `DrawingImage ConvertSvgFileToDrawingImage(string filepath)`

- **`ConvertSvgToDrawingImage`** → `DrawingImage`
  - Converts the SVG to image source.
  - `DrawingImage ConvertSvgToDrawingImage(string svg, string fileName)`

#### 📁 TreeViewBuilder.cs

- **`GetTreeViewItemAsync`** → `Task<TreeViewItem>`
  - Gets the TreeView item.
  - `Task<TreeViewItem> GetTreeViewItemAsync(FExTreeViewNode nodeStub)`

#### 📁 TreeViewBuilderBase.cs (6 methods)

- **`CreateNewNode`** → `void`
  - `void CreateNewNode(FExTreeViewNode node)`

- **`CreateNewNode`** → `void`
  - `void CreateNewNode(string nodePath)`

- **`GetTreeNodeAsync`** → `Task<TItem>`
  - `Task<TItem> GetTreeNodeAsync(string rootNodeName, bool setDirectoriesIcons = false)`

- **`GetTreeViewItemAsync`** → `Task<TItem>`
  - `Task<TItem> GetTreeViewItemAsync(FExTreeViewNode nodeStub)`

- **`GrowTreeAsync`** → `Task`
  - Grows the tree.
  - `Task GrowTreeAsync(ItemsControl tree, TItem newNode, int[] location, int i = 0)`

- **`GrowTreeAsync`** → `Task`
  - Grows the tree.
  - `Task GrowTreeAsync(TItem tree,
                                    FExTreeViewNode nodeStub,
                                    int locationIndex = 0,
                                    bool setDirectoriesIcons = false)`

#### 📁 VirtualizingPanelBase.cs

- **`SetHorizontalOffset`** → `void`
  - `void SetHorizontalOffset(double offset)`

- **`SetVerticalOffset`** → `void`
  - `void SetVerticalOffset(double offset)`

#### 📁 VirtualizingPanelBaseV1.cs

- **`MakeVisible`** → `Rect`
  - The range of items that a realized in viewport or cache.
  - `Rect MakeVisible(Visual visual, Rect rectangle)`

- **`SetHorizontalOffset`** → `void`
  - `void SetHorizontalOffset(double offset)`

- **`SetVerticalOffset`** → `void`
  - `void SetVerticalOffset(double offset)`

#### 📁 VirtualizingPanelModelBase.cs

- **`SetHorizontalOffset`** → `void`
  - `void SetHorizontalOffset(double offset)`

- **`SetVerticalOffset`** → `void`
  - `void SetVerticalOffset(double offset)`

#### 📁 VirtualizingWrapPanelModel.cs

- **`GetAverageItemSize`** → `Size`
  - `Size GetAverageItemSize()`

#### 📁 Win32.cs

- **`GetFileInfo`** → `IntPtr`
  - `IntPtr GetFileInfo(string pszPath,
                                     uint dwFileAttributes,
                                     ref SHFileInfo psfi,
                                     uint cbSizeFileInfo,
                                     uint uFlags)`

### 📊 Properties

- **`AllowDifferentSizedItems`** : `bool`
  - Specifies whether items can have different sizes. The default value is false. If this property is enabled, it is strongly recommended to also set the property. Otherwise, the position of the items is not always guaranteed to be correct.
  - 📁 VirtualizingWrapPanel.cs

- **`Background`** : `Brush`
  - Gets or sets the background.
  - 📁 ViewDesign.cs

- **`BorderBackground`** : `Brush`
  - Gets or sets the header background.
  - 📁 ViewDesign.cs

- **`BusyContent`** : `object`
  - Interaction logic for BusyIndicator.xaml
  - 📁 BusyIndicator.xaml.cs

- **`CbSize`** : `int`
  - The cb size
  - 📁 MONITORINFO.cs

- **`ChildrenSize`** : `Size`
  - Gets or sets a value that specifies whether the items are distributed evenly across the width (horizontal orientation) or height (vertical orientation). The default value is true.
  - 📁 VirtualizingWrapPanelV1.cs

- **`ControlBackground`** : `Brush`
  - Gets or sets the control background.
  - 📁 ViewDesign.cs

- **`DefaultSettings`** : `JsonSerializerSettings`
  - Converts WPF binding error into BindingException
  - 📁 BindingExceptionThrower.cs

- **`DwFlags`** : `int`
  - The dw flags
  - 📁 MONITORINFO.cs

- **`ExpandedItem`** : `object`
  - Gets the currently expanded item. If no item is expanded null is returned.
  - 📁 GridDetailsView.xaml.cs

- **`ExpandedItem`** : `object`
  - Gets or set the expanded item. The default value is null.
  - 📁 VirtualizingWrapPanelWithItemExpansion.cs

- **`ExpandedItemTemplate`** : `DataTemplate`
  - Gets or sets the data template used for the item expansion.
  - 📁 VirtualizingWrapPanelWithItemExpansion.cs

- **`ExpandedItemTemplate`** : `DataTemplate`
  - Gets or sets the data template used for the item expansion.
  - 📁 GridDetailsView.xaml.cs

- **`FontFamily`** : `FontFamily`
  - Gets or sets the size of the font.
  - 📁 ViewDesign.cs

- **`FontSize`** : `double`
  - Gets or sets the size of the font.
  - 📁 ViewDesign.cs

- **`Foreground`** : `Brush`
  - Gets or sets the foreground.
  - 📁 ViewDesign.cs

- **`HeaderBackground`** : `Brush`
  - Gets or sets the header background.
  - 📁 ViewDesign.cs

- **`HorizontalGroupOffset`** : `double`
  - Gets or sets a value that specifies if the items get stretched to fill up remaining space. The default value is false.
  - 📁 VirtualizingWrapPanel.cs

- **`IsBusy`** : `bool`
  - Interaction logic for BusyIndicator.xaml
  - 📁 BusyIndicator.xaml.cs

- **`IsRecycling`** : `bool`
  - Realizes the specified item. If the item is already realized, nothing happens.
  - 📁 ItemContainerManager.cs

- **`IsSpacingEnabled`** : `bool`
  - Gets or sets a value that specifies whether the items are distributed evenly across the width (horizontal orientation) or height (vertical orientation). The default value is true.
  - 📁 VirtualizingWrapPanelV1.cs

- **`IsWrappingKeyboardNavigationEnabled`** : `bool`
  - Enables a improved wrapping keyboard navigation. The default value is false.
  - 📁 GridView.cs

- **`ItemSize`** : `Size`
  - Gets or sets a value that specifies the size of the items. The default value is . If the value is the item size is determined by measuring the first realized item.
  - 📁 VirtualizingWrapPanel.cs

- **`ItemSize`** : `Size`
  - Gets or sets a value that specifies the size of the items. The default value is . If the value is the size of the items gots measured by the first realized item.
  - 📁 VirtualizingWrapPanelV1.cs

- **`ItemSizeProvider`** : `IItemSizeProvider`
  - Specifies an instance of which provides the size of the items. In order to allow different sized items, also enable the property.
  - 📁 VirtualizingWrapPanel.cs

- **`MouseWheelDelta`** : `double`
  - Mouse wheel delta for pixel based scrolling. The default value is 48 dp.
  - 📁 VirtualizingPanelBase.cs

- **`MouseWheelDelta`** : `double`
  - Mouse wheel delta for pixel based scrolling. The default value is 48 dp.
  - 📁 VirtualizingPanelBaseV1.cs

- **`MouseWheelDeltaItem`** : `int`
  - Mouse wheel delta for item based scrolling. The default value is 3 items.
  - 📁 VirtualizingPanelBase.cs

- **`MouseWheelDeltaItem`** : `int`
  - Mouse wheel delta for item based scrolling. The default value is 3 items.
  - 📁 VirtualizingPanelBaseV1.cs

- **`OccurenceTime`** : `DateTime`
  - Exception thrown by the BindingExceptionThrower each time a WPF binding error occurs
  - 📁 BindingException.cs

- **`Orientation`** : `Orientation`
  - Gets or sets a value that specifies the orientation in which items are arranged. The default value is .
  - 📁 VirtualizingWrapPanelV1.cs

- **`Orientation`** : `Orientation`
  - Gets or sets a value that specifies the orientation in which items are arranged. The default value is .
  - 📁 GridView.cs

- **`Orientation`** : `Orientation`
  - Gets or sets a value that specifies the orientation in which items are arranged. The default value is .
  - 📁 VirtualizingWrapPanel.cs

- **`RcMonitor`** : `RectStruct`
  - The rc monitor
  - 📁 MONITORINFO.cs

- **`RcWork`** : `RectStruct`
  - The rc work
  - 📁 MONITORINFO.cs

- **`ScrollLineDelta`** : `double`
  - Scroll line delta for pixel based scrolling. The default value is 16 dp.
  - 📁 VirtualizingPanelBaseV1.cs

- **`ScrollLineDelta`** : `double`
  - Scroll line delta for pixel based scrolling. The default value is 16 dp.
  - 📁 VirtualizingPanelBase.cs

- **`ScrollLineDeltaItem`** : `double`
  - Scroll line delta for item based scrolling. The default value is 1 item.
  - 📁 VirtualizingPanelBaseV1.cs

- **`ScrollLineDeltaItem`** : `int`
  - Scroll line delta for item based scrolling. The default value is 1 item.
  - 📁 VirtualizingPanelBase.cs

- **`SpacingMode`** : `SpacingMode`
  - Gets or sets the spacing mode used when arranging the items. The default value is .
  - 📁 VirtualizingWrapPanel.cs

- **`SpacingMode`** : `SpacingMode`
  - Gets or sets the spacing mode used when arranging the items. The default value is .
  - 📁 VirtualizingWrapPanelV1.cs

- **`SpacingMode`** : `SpacingMode`
  - Gets or sets the spacing mode used when arranging the items. The default value is .
  - 📁 GridView.cs

- **`StackTrace`** : `string`
  - Exception thrown by the BindingExceptionThrower each time a WPF binding error occurs
  - 📁 BindingException.cs

- **`StretchItems`** : `bool`
  - Gets or sets a value that specifies if the items get stretched to fill up remaining space. The default value is false.
  - 📁 GridView.cs

- **`StretchItems`** : `bool`
  - Gets or sets a value that specifies if the items get stretched to fill up remaining space. The default value is false.
  - 📁 VirtualizingWrapPanelV1.cs

- **`StretchItems`** : `bool`
  - Gets or sets a value that specifies if the items get stretched to fill up remaining space. The default value is false.
  - 📁 VirtualizingWrapPanel.cs

---


## 🚀 Usage Instructions

### For AI Agents

1. **Load this catalog** at the start of working with FEx-consuming projects
2. **Reference APIs** by their full signatures when suggesting code
3. **Prefer FEx utilities** over reimplementing common functionality
4. **Check extension methods** - FEx provides rich extensions for strings, collections, enums, etc.

### For Developers

`powershell
# Regenerate catalog after code changes
.\Generate-FExCatalog.ps1

# View with detailed logging
.\Generate-FExCatalog.ps1 -Verbose
`

### Example Usage Prompt

`
Load API catalog: X:\GitLab\Flakroup\FEx\FEx-API-Catalog.md

I'm working on a project that references FEx framework. 
Please review available utilities before implementing new code.
`

---

*Catalog generated by `Generate-FExCatalog.ps1`*
