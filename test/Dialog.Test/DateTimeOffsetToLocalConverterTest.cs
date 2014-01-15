using System;
using System.Collections.Generic;
using System.Globalization;
using NuGet.Dialog.PackageManagerUI;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Dialog.Test
{
    public class DateTimeOffsetToLocalConverterTest
    {
        [Theory,
        InlineData(0),
        InlineData(1),
        InlineData(-10),
        InlineData(-5),
        InlineData(-14),
        InlineData(14)]
        public void ConvertDateTimeOffsetToLocal(int offset)
        {
            // Arrange
            DateTimeOffsetToLocalConverter converter = new DateTimeOffsetToLocalConverter();
            DateTime utcDateTime = new DateTime(2013, 4, 16, 3, 55, 30, DateTimeKind.Utc);
            DateTimeOffset utcOffset = new DateTimeOffset(utcDateTime);
            DateTimeOffset expected = utcDateTime.ToLocalTime();

            var date = utcOffset.ToOffset(new TimeSpan(offset, 0, 0));

            // Act
            var actual = (DateTimeOffset)converter.Convert(date, typeof(DateTimeOffset), null, CultureInfo.InvariantCulture);

            // Assert that the offsets are identical
            Assert.True(expected.Offset.CompareTo(actual.Offset) == 0);

            // Assert that the dates display the same
            Assert.Equal<string>(expected.ToString("d"), actual.ToString("d"));
        }

        [Fact]
        public void ConvertNullDateTimeOffset()
        {
            // Arrange
            DateTimeOffsetToLocalConverter converter = new DateTimeOffsetToLocalConverter();

            // Act
            DateTimeOffset? a = converter.Convert(null, typeof(DateTimeOffset), null, CultureInfo.InvariantCulture) as DateTimeOffset?;
            DateTimeOffset? b = converter.Convert(null, null, null, CultureInfo.InvariantCulture) as DateTimeOffset?;

            // Assert - Null should be returned without any exceptions thrown
            Assert.False(a.HasValue);
            Assert.False(b.HasValue);
        }

        [Fact]
        public void ConvertEmptyDateTimeOffset()
        {
            // Arrange
            DateTimeOffsetToLocalConverter converter = new DateTimeOffsetToLocalConverter();
            DateTimeOffset empty = new DateTimeOffset(0, new TimeSpan(0, 0, 0));

            // Act & Assert
            Assert.DoesNotThrow(() => converter.Convert(empty, typeof(DateTimeOffset), null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void ConvertNonDateTimeOffset()
        {
            // Arrange
            DateTimeOffsetToLocalConverter converter = new DateTimeOffsetToLocalConverter();
            DateTime now = DateTime.UtcNow;

            // Act
            DateTime? a = converter.Convert(now, typeof(DateTime), null, CultureInfo.InvariantCulture) as DateTime?;

            // Assert
            Assert.True(a.HasValue);
            Assert.True(now.CompareTo(a.Value) == 0);
        }
    }
}
