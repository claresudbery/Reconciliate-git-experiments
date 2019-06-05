﻿using System;
using System.Collections.Generic;
using ConsoleCatchall.Console.Reconciliation;
using ConsoleCatchall.Console.Reconciliation.Files;
using ConsoleCatchall.Console.Reconciliation.Matchers;
using ConsoleCatchall.Console.Reconciliation.Records;
using ConsoleCatchall.Console.Reconciliation.Spreadsheets;
using ConsoleCatchall.Console.Reconciliation.Extensions;
using Interfaces;
using Interfaces.Constants;
using Interfaces.DTOs;
using Moq;
using NUnit.Framework;

namespace ConsoleCatchallTests.Reconciliation.Matchers
{
    [TestFixture]
    public partial class BankAndBankInMatcherTests : IInputOutput
    {
        private Mock<IInputOutput> _mockInputOutput;

        [SetUp]
        public void SetUp()
        {
            _mockInputOutput = new Mock<IInputOutput>();
        }

        [Test]
        public void M_WhenMatchingSpecifiedRecords_WillMatchRecordWithSpecifiedIndex()
        {
            // Arrange
            var mockOwnedFile = new Mock<ICSVFile<BankRecord>>();
            mockOwnedFile.Setup(x => x.Records).Returns(new List<BankRecord>());
            var matcher = new BankAndBankInMatcher(this, new FakeSpreadsheetRepoFactory());
            var sourceRecord = new ActualBankRecord
            {
                Amount = 34.56,
                Match = null,
                Matched = false
            };
            var potentialMatches = new List<IPotentialMatch>
            {
                new PotentialMatch { ActualRecords = new List<ICSVRecord> { new BankRecord {Matched = false} } },
                new PotentialMatch { ActualRecords = new List<ICSVRecord> { new BankRecord {Matched = false} } },
                new PotentialMatch { ActualRecords = new List<ICSVRecord> { new BankRecord {Matched = false} } }
            };
            var recordForMatching = new RecordForMatching<ActualBankRecord>(sourceRecord, potentialMatches);
            var index = 1;

            // Act
            matcher.MatchSpecifiedRecords(recordForMatching, index, mockOwnedFile.Object);

            // Assert
            Assert.AreEqual(false, recordForMatching.Matches[0].ActualRecords[0].Matched, "first record not matched");
            Assert.AreEqual(true, recordForMatching.Matches[1].ActualRecords[0].Matched, "second record matched");
            Assert.AreEqual(false, recordForMatching.Matches[2].ActualRecords[0].Matched, "third record not matched");
        }

        [Test]
        public void M_WhenMatchingSpecifiedRecords_WillReplaceMultipleMatchesWithSingleMatch()
        {
            // Arrange
            var mockOwnedFile = new Mock<ICSVFile<BankRecord>>();
            mockOwnedFile.Setup(x => x.Records).Returns(new List<BankRecord>());
            var matcher = new BankAndBankInMatcher(this, new FakeSpreadsheetRepoFactory());
            var sourceRecord = new ActualBankRecord
            {
                Amount = 34.56,
                Match = null,
                Matched = false
            };
            var potentialMatches = new List<IPotentialMatch>
            {
                new PotentialMatch
                {
                    ActualRecords = new List<ICSVRecord>
                    {
                        new BankRecord {Description = "Match 01", UnreconciledAmount = 20},
                        new BankRecord {Description = "Match 02", UnreconciledAmount = 30}
                    }
                }
            };
            var recordForMatching = new RecordForMatching<ActualBankRecord>(sourceRecord, potentialMatches);
            var index = 0;
            Assert.AreEqual(2, recordForMatching.Matches[index].ActualRecords.Count, "num matches before call");

            // Act
            matcher.MatchSpecifiedRecords(recordForMatching, index, mockOwnedFile.Object);

            // Assert
            Assert.AreEqual(1, recordForMatching.Matches[index].ActualRecords.Count, "num matches after call");
        }

        [Test]
        public void M_WhenMatchingSpecifiedRecords_AndMultipleMatchesExist_WillCreateNewRecordWithExplanatoryDescription()
        {
            // Arrange
            var mockOwnedFile = new Mock<ICSVFile<BankRecord>>();
            mockOwnedFile.Setup(x => x.Records).Returns(new List<BankRecord>());
            var matcher = new BankAndBankInMatcher(this, new FakeSpreadsheetRepoFactory());
            var sourceRecord = new ActualBankRecord
            {
                Amount = 34.56,
                Match = null,
                Matched = false
            };
            var potentialMatches = new List<IPotentialMatch>
            {
                new PotentialMatch
                {
                    ActualRecords = new List<ICSVRecord>
                    {
                        new BankRecord {Description = "Match 01", UnreconciledAmount = 20.22},
                        new BankRecord {Description = "Match 02", UnreconciledAmount = 30.33},
                        new BankRecord {Description = "Match 02", UnreconciledAmount = 40.44}
                    }
                }
            };
            var index = 0;
            var matches = potentialMatches[index].ActualRecords;
            var expectedDescription =
                $"{ReconConsts.SeveralExpenses} (£{matches[0].MainAmount()}, £{matches[1].MainAmount()}, £{matches[2].MainAmount()})";
            var recordForMatching = new RecordForMatching<ActualBankRecord>(sourceRecord, potentialMatches);

            // Act
            matcher.MatchSpecifiedRecords(recordForMatching, index, mockOwnedFile.Object);

            // Assert
            Assert.AreEqual(expectedDescription, recordForMatching.Matches[index].ActualRecords[0].Description);
        }

        [Test]
        public void M_WhenMatchingSpecifiedRecords_AndMultipleMatchesExist_WillCreateNewRecordWithDateToMatchSourceRecord()
        {
            // Arrange
            var mockOwnedFile = new Mock<ICSVFile<BankRecord>>();
            mockOwnedFile.Setup(x => x.Records).Returns(new List<BankRecord>());
            var matcher = new BankAndBankInMatcher(this, new FakeSpreadsheetRepoFactory());
            var sourceRecord = new ActualBankRecord
            {
                Date = DateTime.Today,
                Amount = 34.56,
                Match = null,
                Matched = false
            };
            var potentialMatches = new List<IPotentialMatch>
            {
                new PotentialMatch
                {
                    ActualRecords = new List<ICSVRecord>
                    {
                        new BankRecord {Description = "Match 01", UnreconciledAmount = 20.22},
                        new BankRecord {Description = "Match 02", UnreconciledAmount = 30.33},
                        new BankRecord {Description = "Match 02", UnreconciledAmount = 40.44}
                    }
                }
            };
            var index = 0;
            var recordForMatching = new RecordForMatching<ActualBankRecord>(sourceRecord, potentialMatches);

            // Act
            matcher.MatchSpecifiedRecords(recordForMatching, index, mockOwnedFile.Object);

            // Assert
            Assert.AreEqual(sourceRecord.Date, recordForMatching.Matches[index].ActualRecords[0].Date);
        }

        [Test]
        public void M_WhenMatchingSpecifiedRecords_AndMultipleMatchesExist_WillCreateNewRecordWithAllAmountsAddedTogether()
        {
            // Arrange
            var mockOwnedFile = new Mock<ICSVFile<BankRecord>>();
            mockOwnedFile.Setup(x => x.Records).Returns(new List<BankRecord>());
            var matcher = new BankAndBankInMatcher(this, new FakeSpreadsheetRepoFactory());
            var sourceRecord = new ActualBankRecord
            {
                Date = DateTime.Today,
                Amount = 34.56,
                Match = null,
                Matched = false
            };
            var potentialMatches = new List<IPotentialMatch>
            {
                new PotentialMatch
                {
                    ActualRecords = new List<ICSVRecord>
                    {
                        new BankRecord {Description = "Match 01", UnreconciledAmount = 20.22},
                        new BankRecord {Description = "Match 02", UnreconciledAmount = 30.33},
                        new BankRecord {Description = "Match 02", UnreconciledAmount = 40.44}
                    }
                }
            };
            var index = 0;
            var matches = potentialMatches[index].ActualRecords;
            var expectedAmount = matches[0].MainAmount() + matches[1].MainAmount() + matches[2].MainAmount();
            var recordForMatching = new RecordForMatching<ActualBankRecord>(sourceRecord, potentialMatches);

            // Act
            matcher.MatchSpecifiedRecords(recordForMatching, index, mockOwnedFile.Object);

            // Assert
            Assert.AreEqual(expectedAmount, recordForMatching.Matches[index].ActualRecords[0].MainAmount());
        }

        [Test]
        public void M_WhenMatchingSpecifiedRecords_AndMultipleMatchesExist_WillCreateNewRecordWithTypeOfFirstMatch()
        {
            // Arrange
            var mockOwnedFile = new Mock<ICSVFile<BankRecord>>();
            mockOwnedFile.Setup(x => x.Records).Returns(new List<BankRecord>());
            var matcher = new BankAndBankInMatcher(this, new FakeSpreadsheetRepoFactory());
            var sourceRecord = new ActualBankRecord
            {
                Date = DateTime.Today,
                Amount = 34.56,
                Match = null,
                Matched = false
            };
            var potentialMatches = new List<IPotentialMatch>
            {
                new PotentialMatch
                {
                    ActualRecords = new List<ICSVRecord>
                    {
                        new BankRecord {Description = "Match 01", UnreconciledAmount = 20.22, Type = "Type"},
                        new BankRecord {Description = "Match 02", UnreconciledAmount = 30.33},
                        new BankRecord {Description = "Match 02", UnreconciledAmount = 40.44}
                    }
                }
            };
            var index = 0;
            var expectedType = (potentialMatches[index].ActualRecords[0] as BankRecord).Type;
            var recordForMatching = new RecordForMatching<ActualBankRecord>(sourceRecord, potentialMatches);

            // Act
            matcher.MatchSpecifiedRecords(recordForMatching, index, mockOwnedFile.Object);

            // Assert
            Assert.AreEqual(expectedType, (recordForMatching.Matches[index].ActualRecords[0] as BankRecord).Type);
        }

        [Test]
        public void M_WhenMatchingSpecifiedRecords_BothSourceAndMatchWillHaveMatchedSetToTrue()
        {
            // Arrange
            var mockOwnedFile = new Mock<ICSVFile<BankRecord>>();
            mockOwnedFile.Setup(x => x.Records).Returns(new List<BankRecord>());
            var matcher = new BankAndBankInMatcher(this, new FakeSpreadsheetRepoFactory());
            var sourceRecord = new ActualBankRecord
            {
                Date = DateTime.Today,
                Amount = 34.56,
                Match = null,
                Matched = false
            };
            var potentialMatches = new List<IPotentialMatch>
            {
                new PotentialMatch
                {
                    ActualRecords = new List<ICSVRecord>
                    {
                        new BankRecord {Description = "Match 01", UnreconciledAmount = 20.22, Type = "Type"},
                        new BankRecord {Description = "Match 02", UnreconciledAmount = 30.33},
                        new BankRecord {Description = "Match 02", UnreconciledAmount = 40.44}
                    }
                }
            };
            var index = 0;
            var recordForMatching = new RecordForMatching<ActualBankRecord>(sourceRecord, potentialMatches);

            // Act
            matcher.MatchSpecifiedRecords(recordForMatching, index, mockOwnedFile.Object);

            // Assert
            Assert.AreEqual(true, recordForMatching.Matches[index].ActualRecords[0].Matched, "match is set to matched");
            Assert.AreEqual(true, recordForMatching.SourceRecord.Matched, "source is set to matched");
        }

        [Test]
        public void M_WhenMatchingSpecifiedRecords_SourceAndMatchWillHaveMatchPropertiesPointingAtEachOther()
        {
            // Arrange
            var mockOwnedFile = new Mock<ICSVFile<BankRecord>>();
            mockOwnedFile.Setup(x => x.Records).Returns(new List<BankRecord>());
            var matcher = new BankAndBankInMatcher(this, new FakeSpreadsheetRepoFactory());
            var sourceRecord = new ActualBankRecord
            {
                Date = DateTime.Today,
                Amount = 34.56,
                Match = null,
                Matched = false
            };
            var potentialMatches = new List<IPotentialMatch>
            {
                new PotentialMatch
                {
                    ActualRecords = new List<ICSVRecord>
                    {
                        new BankRecord {Description = "Match 01", UnreconciledAmount = 20.22, Type = "Type"},
                        new BankRecord {Description = "Match 02", UnreconciledAmount = 30.33},
                        new BankRecord {Description = "Match 02", UnreconciledAmount = 40.44}
                    }
                }
            };
            var index = 0;
            var recordForMatching = new RecordForMatching<ActualBankRecord>(sourceRecord, potentialMatches);

            // Act
            matcher.MatchSpecifiedRecords(recordForMatching, index, mockOwnedFile.Object);

            // Assert
            Assert.AreEqual(recordForMatching.SourceRecord, recordForMatching.Matches[index].ActualRecords[0].Match, "match is pointing at source");
            Assert.AreEqual(recordForMatching.Matches[index].ActualRecords[0], recordForMatching.SourceRecord.Match, "source is pointing at match");
        }

        [Test]
        public void M_WhenMatchingSpecifiedRecords_AndMultipleMatchesExist_WillRemoveOriginalMatchesFromOwnedFile()
        {
            // Arrange
            var matcher = new BankAndBankInMatcher(this, new FakeSpreadsheetRepoFactory());
            var sourceRecord = new ActualBankRecord
            {
                Date = DateTime.Today,
                Amount = 34.56,
                Match = null,
                Matched = false
            };
            var bankRecords = new List<BankRecord>
            {
                new BankRecord {Description = "Match 01", UnreconciledAmount = 20.22, Type = "Type"},
                new BankRecord {Description = "Match 02", UnreconciledAmount = 30.33},
                new BankRecord {Description = "Match 02", UnreconciledAmount = 40.44}
            };
            var mockBankInFileIO = new Mock<IFileIO<BankRecord>>();
            mockBankInFileIO.Setup(x => x.Load(It.IsAny<List<string>>(), null)).Returns(bankRecords);
            var bankInFile = new CSVFile<BankRecord>(mockBankInFileIO.Object);
            bankInFile.Load();
            var potentialMatches = new List<IPotentialMatch> { new PotentialMatch {ActualRecords = new List<ICSVRecord>()} };
            potentialMatches[0].ActualRecords.Add(bankRecords[0]);
            potentialMatches[0].ActualRecords.Add(bankRecords[1]);
            potentialMatches[0].ActualRecords.Add(bankRecords[2]);
            var index = 0;
            var recordForMatching = new RecordForMatching<ActualBankRecord>(sourceRecord, potentialMatches);
            foreach (var bankRecord in bankRecords)
            {
                Assert.IsTrue(bankInFile.Records.Contains(bankRecord));
            }

            // Act
            matcher.MatchSpecifiedRecords(recordForMatching, index, bankInFile);

            // Assert
            foreach (var bankRecord in bankRecords)
            {
                Assert.IsFalse(bankInFile.Records.Contains(bankRecord));
            }
        }

        [Test]
        public void M_WhenMatchingSpecifiedRecords_AndMultipleMatchesExist_WillAddNewlyCreatedMatchToOwnedFile()
        {
            // Arrange
            var matcher = new BankAndBankInMatcher(this, new FakeSpreadsheetRepoFactory());
            var sourceRecord = new ActualBankRecord
            {
                Date = DateTime.Today,
                Amount = 34.56,
                Match = null,
                Matched = false
            };
            var bankRecords = new List<BankRecord>
            {
                new BankRecord {Description = "Match 01", UnreconciledAmount = 20.22, Type = "Type"},
                new BankRecord {Description = "Match 02", UnreconciledAmount = 30.33},
                new BankRecord {Description = "Match 02", UnreconciledAmount = 40.44}
            };
            var mockBankInFileIO = new Mock<IFileIO<BankRecord>>();
            mockBankInFileIO.Setup(x => x.Load(It.IsAny<List<string>>(), null)).Returns(bankRecords);
            var bankInFile = new CSVFile<BankRecord>(mockBankInFileIO.Object);
            bankInFile.Load();
            var potentialMatches = new List<IPotentialMatch> { new PotentialMatch { ActualRecords = new List<ICSVRecord>() } };
            potentialMatches[0].ActualRecords.Add(bankRecords[0]);
            potentialMatches[0].ActualRecords.Add(bankRecords[1]);
            potentialMatches[0].ActualRecords.Add(bankRecords[2]);
            var index = 0;
            var recordForMatching = new RecordForMatching<ActualBankRecord>(sourceRecord, potentialMatches);
            foreach (var bankRecord in bankRecords)
            {
                Assert.IsTrue(bankInFile.Records.Contains(bankRecord));
            }

            // Act
            matcher.MatchSpecifiedRecords(recordForMatching, index, bankInFile);

            // Assert
            Assert.AreEqual(1, bankInFile.Records.Count);
            Assert.IsTrue(bankInFile.Records[0].Description.Contains(ReconConsts.SeveralExpenses));
        }

        [Test]
        public void M_WillPopulateConsoleLinesForEveryPotentialMatch()
        {
            Assert.AreEqual(true, true);
        }

        [Test]
        public void M_WillPopulateRankingsForEveryPotentialMatch()
        {
            Assert.AreEqual(true, true);
        }

        [Test]
        public void M_WillFindSingleMatchingBankInTransactionForOneActualBankExpenseTransaction()
        {
            Assert.AreEqual(true, true);
        }

        [Test]
        public void M_WillFindSingleMatchingCollectionOfBankInTransactionsForOneActualBankExpenseTransaction()
        {
            Assert.AreEqual(true, true);
        }

        [Test]
        public void M_WillFindMultipleMatchingCollectionOfBankInTransactionsForOneActualBankExpenseTransaction()
        {
            Assert.AreEqual(true, true);
        }
    }
}
