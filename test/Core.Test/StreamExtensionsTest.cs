using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Test
{
    public class StreamExtensionsTest
    {
        [Fact]
        public void ContentEqualsReturnPositiveResult1()
        {
            // Arrange
            string first = @"this is awesome.";
            string second = @"this is awesome.";

            // Act
            bool equal = StreamExtensions.ContentEquals(first.AsStream(), second.AsStream());

            // Assert
            Assert.True(equal);
        }

        [Fact]
        public void ContentEqualsReturnPositiveResult2()
        {
            // Arrange
            string first = @"this is awesome.
-0------- NUGET: BEGIN LICENSE TEXT    
*******NUGET: END LICENSE TEXT DSA F DASF DSADSA FDAS F DSAF DAS";
            string second = @"this is awesome.";

            // Act
            bool equal = StreamExtensions.ContentEquals(first.AsStream(), second.AsStream());

            // Assert
            Assert.True(equal);
        }

        [Fact]
        public void ContentEqualsReturnPositiveResult3()
        {
            // Arrange
            string first = @"this is awesome.
-0------- NUGET: BEGIN LICENSE TEXT    
NUGET: END LICENSE TEXT DSA F DASF DSADSA FDAS F DSAF DAS
holiday season is over.";
            string second = @"********NUGET: BEGIN LICENSE TEXT
SADKLFJ;LDSAJKFDSAFDSAFDAS
DSALK;FJL;KDSAJFL;KDSJAFL;KDSA
FSDAKLJF;LKDSAJFLK;DSAJFASD
DSALKJFLK;DSAJFDSA
         ***  NUGET: end License Text ----SDALFJ;LDSAKJFLK;DJSAL;FKJDSLAK;FDSA
this is awesome.
holiday season is over.";

            // Act
            bool equal = StreamExtensions.ContentEquals(first.AsStream(), second.AsStream());

            // Assert
            Assert.True(equal);
        }

        [Fact]
        public void ContentEqualsReturnPositiveResult4()
        {
            // Arrange
            string first = @"this is awesome.
holiday season is over.";
            string second = @"******** NUGET: BEGIN LICENSE TEXT
SADKLFJ;LDSAJKFDSAFDSAFDAS
DSALK;FJL;KDSAJFL;KDSJAFL;KDSA
FSDAKLJF;LKDSAJFLK;DSAJFASD
DSALKJFLK;DSAJFDSA
              NUGET: end License Text ----SDALFJ;LDSAKJFLK;DJSAL;FKJDSLAK;FDSA
this is awesome.
holiday season is over.";

            // Act
            bool equal = StreamExtensions.ContentEquals(first.AsStream(), second.AsStream());

            // Assert
            Assert.True(equal);
        }

        [Fact]
        public void ContentEqualsReturnPositiveResult5()
        {
            // Arrange
            string first = @"NUGET: BEGIN LICENSE TEXT
dsalkfjdlasjflkdsajfl;kdsa
dsaklfj;ldksajflk;dsjal;fkjdsal;k
dsafkljd;sakljfl;kdsajfdsa
dsaklfjl;dksajflk;dsajflk;sd
    NUGET: END LICENSE TEXT";
            string second = @"******** nuget: begin license text
SADKLFJ;LDSAJKFDSAFDSAFDAS
DSALK;FJL;KDSAJFL;KDSJAFL;KDSA
FSDAKLJF;LKDSAJFLK;DSAJFASD
DSALKJFLK;DSAJFDSA
              NUGET: END LICENSE TEXT ----SDALFJ;LDSAKJFLK;DJSAL;FKJDSLAK;FDSA";

            // Act
            bool equal = StreamExtensions.ContentEquals(first.AsStream(), second.AsStream());

            // Assert
            Assert.True(equal);
        }

        [Fact]
        public void ContentEqualsReturnPositiveResult6()
        {
            // Arrange
            string first = @"two
nuget: begin license text
NUGET: BEGIN LICENSE TEXT
NUGET: end License Text";
            string second = @"two
nuget: begin license text
SADKLFJ;LDSAJKFDSAFDSAFDAS
DSALK;FJL;KDSAJFL;KDSJAFL;KDSA
FSDAKLJF;LKDSAJFLK;DSAJFASD
DSALKJFLK;DSAJFDSA
              ----SDALFJ;LDSAKJFLK;DJSAL;FKJDSLAK;FDSA
NUGET: END LICENSE TEXT";

            // Act
            bool equal = StreamExtensions.ContentEquals(first.AsStream(), second.AsStream());

            // Assert
            Assert.True(equal);
        }

        [Fact]
        public void ContentEqualsReturnPositiveResult7()
        {
            // Arrange
            string first = @"one
nuget: begin license text
sdajfl;kdsajfl;dskjaf;ldsa
NUGET: end License Text
two
------------nuget: begin license text----------------------
sdajfl;kdsajfl;dskjaf;ldsa
*************NUGET: end License Text*******************";
            string second = @"one
nuget: begin license text
SADKLFJ;LDSAJKFDSAFDSAFDAS
DSALK;FJL;KDSAJFL;KDSJAFL;KDSA
FSDAKLJF;LKDSAJFLK;DSAJFASD
DSALKJFLK;DSAJFDSA
              ----SDALFJ;LDSAKJFLK;DJSAL;FKJDSLAK;FDSA
NUGET: END LICENSE TEXT
two";

            // Act
            bool equal = StreamExtensions.ContentEquals(first.AsStream(), second.AsStream());

            // Assert
            Assert.True(equal);
        }

        [Fact]
        public void ContentEqualsReturnNegativeResult1()
        {
            // Arrange
            string first = @"NUGET: BEGIN LICENSE TEXT
dsalkfjdlasjflkdsajfl;kdsa
dsaklfj;ldksajflk;dsjal;fkjdsal;k
dsafkljd;sakljfl;kdsajfdsa
dsaklfjl;dksajflk;dsajflk;sd
    NUGET: END LICENSE TEXT";
            string second = @"******** BEGIN NUGET IGNOR
SADKLFJ;LDSAJKFDSAFDSAFDAS
DSALK;FJL;KDSAJFL;KDSJAFL;KDSA
FSDAKLJF;LKDSAJFLK;DSAJFASD
DSALKJFLK;DSAJFDSA
              NUGET: END LICENSE TEXT ----SDALFJ;LDSAKJFLK;DJSAL;FKJDSLAK;FDSA";

            // Act
            bool equal = StreamExtensions.ContentEquals(first.AsStream(), second.AsStream());

            // Assert
            Assert.False(equal);
        }

        [Fact]
        public void ContentEqualsReturnNegativeResult2()
        {
            // Arrange
            string first = @"NUGET: BEGIN LICENSE TEXT
dsalkfjdlasjflkdsajfl;kdsa
dsaklfj;ldksajflk;dsjal;fkjdsal;k
dsafkljd;sakljfl;kdsajfdsa
dsaklfjl;dksajflk;dsajflk;sd
    NUGET: end License Text
two";
            string second = @"one
******** NUGET: BEGIN LICENSE TEXT
SADKLFJ;LDSAJKFDSAFDSAFDAS
DSALK;FJL;KDSAJFL;KDSJAFL;KDSA
FSDAKLJF;LKDSAJFLK;DSAJFASD
DSALKJFLK;DSAJFDSA
              NUGET: END LICENSE TEXT ----SDALFJ;LDSAKJFLK;DJSAL;FKJDSLAK;FDSA";

            // Act
            bool equal = StreamExtensions.ContentEquals(first.AsStream(), second.AsStream());

            // Assert
            Assert.False(equal);
        }

        [Fact]
        public void ContentEqualsReturnNegativeResult3()
        {
            // Arrange
            string first = @"two";
            string second = @"two
******** nuget: begin license text
SADKLFJ;LDSAJKFDSAFDSAFDAS
DSALK;FJL;KDSAJFL;KDSJAFL;KDSA
FSDAKLJF;LKDSAJFLK;DSAJFASD
DSALKJFLK;DSAJFDSA
              ----SDALFJ;LDSAKJFLK;DJSAL;FKJDSLAK;FDSA";

            // Act
            bool equal = StreamExtensions.ContentEquals(first.AsStream(), second.AsStream());

            // Assert
            Assert.True(equal);
        }

        [Fact]
        public void ContentEqualsReturnNegativeResult4()
        {
            // Arrange
            string first = @"two";
            string second = @"two
NUGET: END LICENSE TEXT
SADKLFJ;LDSAJKFDSAFDSAFDAS
DSALK;FJL;KDSAJFL;KDSJAFL;KDSA
FSDAKLJF;LKDSAJFLK;DSAJFASD
DSALKJFLK;DSAJFDSA
              ----SDALFJ;LDSAKJFLK;DJSAL;FKJDSLAK;FDSA";

            // Act
            bool equal = StreamExtensions.ContentEquals(first.AsStream(), second.AsStream());

            // Assert
            Assert.False(equal);
        }

        public void ContentEqualsReturnEndResult5()
        {
            // Arrange
            string first = @"two
NUGET: BEGIN LICENSE TEXT
NUGET: BEGIN LICENSE TEXT
NUGET: end License Text
NUGET: END LICENSE TEXT";
            string second = @"two
NUGET: BEGIN LICENSE TEXT
SADKLFJ;LDSAJKFDSAFDSAFDAS
DSALK;FJL;KDSAJFL;KDSJAFL;KDSA
FSDAKLJF;LKDSAJFLK;DSAJFASD
DSALKJFLK;DSAJFDSA
              ----SDALFJ;LDSAKJFLK;DJSAL;FKJDSLAK;FDSA
----NUGET: END LICENSE TEXT";

            // Act
            bool equal = StreamExtensions.ContentEquals(first.AsStream(), second.AsStream());

            // Assert
            Assert.False(equal);
        }
    }
}
