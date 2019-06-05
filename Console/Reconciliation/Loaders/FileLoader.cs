﻿using System;
using System.Globalization;
using ConsoleCatchall.Console.Reconciliation.Files;
using ConsoleCatchall.Console.Reconciliation.Spreadsheets;
using Interfaces;
using Interfaces.Constants;
using Interfaces.DTOs;

namespace ConsoleCatchall.Console.Reconciliation.Loaders
{
    internal class FileLoader
    {
        public readonly IInputOutput _inputOutput;

        public FileLoader(IInputOutput inputOutput)
        {
            _inputOutput = inputOutput;
        }

        public void MergeBudgetData<TThirdPartyType, TOwnedType>(
                ISpreadsheet spreadsheet,
                ICSVFile<TOwnedType> pendingFile,
                BudgetingMonths budgetingMonths,
                DataLoadingInformation<TThirdPartyType, TOwnedType> dataLoadingInfo)
                where TThirdPartyType : ICSVRecord, new()
            where TOwnedType : ICSVRecord, new()
        {
            _inputOutput.OutputLine("Merging budget data with pending data...");
            spreadsheet.AddBudgetedMonthlyDataToPendingFile(budgetingMonths, pendingFile, dataLoadingInfo.MonthlyBudgetData);
            if (null != dataLoadingInfo.AnnualBudgetData)
            {
                spreadsheet.AddBudgetedAnnualDataToPendingFile(budgetingMonths, pendingFile, dataLoadingInfo.AnnualBudgetData);
            }
        }

        public void MergeOtherData<TThirdPartyType, TOwnedType>(
                ISpreadsheet spreadsheet,
                ICSVFile<TOwnedType> pendingFile,
                BudgetingMonths budgetingMonths,
                DataLoadingInformation<TThirdPartyType, TOwnedType> dataLoadingInfo)
            where TThirdPartyType : ICSVRecord, new()
            where TOwnedType : ICSVRecord, new()
        {
            dataLoadingInfo.Loader.MergeBespokeDataWithPendingFile(_inputOutput, spreadsheet, pendingFile, budgetingMonths, dataLoadingInfo);
        }

        public void MergeUnreconciledData<TThirdPartyType, TOwnedType>(
                ISpreadsheet spreadsheet,
                ICSVFile<TOwnedType> pendingFile,
                DataLoadingInformation<TThirdPartyType, TOwnedType> dataLoadingInfo)
            where TThirdPartyType : ICSVRecord, new()
            where TOwnedType : ICSVRecord, new()
        {
            _inputOutput.OutputLine("Merging unreconciled rows from spreadsheet with pending and budget data...");
            spreadsheet.AddUnreconciledRowsToCsvFile<TOwnedType>(dataLoadingInfo.SheetName, pendingFile);

            _inputOutput.OutputLine("Copying merged data (from pending, unreconciled, and budgeting) into main 'owned' csv file...");
            pendingFile.UpdateSourceLinesForOutput(dataLoadingInfo.LoadingSeparator);
            pendingFile.WriteToFileAsSourceLines(dataLoadingInfo.FilePaths.OwnedFileName);

            _inputOutput.OutputLine("...");
        }

        public Reconciliator<TThirdPartyType, TOwnedType>
            LoadThirdPartyAndOwnedFilesIntoReconciliator<TThirdPartyType, TOwnedType>(
                DataLoadingInformation<TThirdPartyType, TOwnedType> dataLoadingInfo,
                IFileIO<TThirdPartyType> thirdPartyFileIO,
                IFileIO<TOwnedType> ownedFileIO)
            where TThirdPartyType : ICSVRecord, new() where TOwnedType : ICSVRecord, new()
        {
            _inputOutput.OutputLine("Loading data back in from 'owned' and 'third party' files...");
            thirdPartyFileIO.SetFilePaths(dataLoadingInfo.FilePaths.MainPath, dataLoadingInfo.FilePaths.ThirdPartyFileName);
            ownedFileIO.SetFilePaths(dataLoadingInfo.FilePaths.MainPath, dataLoadingInfo.FilePaths.OwnedFileName);
            var thirdPartyFile = dataLoadingInfo.Loader.CreateNewThirdPartyFile(thirdPartyFileIO);
            var ownedFile = dataLoadingInfo.Loader.CreateNewOwnedFile(ownedFileIO);

            var reconciliator = new Reconciliator<TThirdPartyType, TOwnedType>(
                dataLoadingInfo,
                thirdPartyFile,
                ownedFile);

            return reconciliator;
        }

        public ReconciliationInterface<TThirdPartyType, TOwnedType>
            CreateReconciliationInterface<TThirdPartyType, TOwnedType>(
                DataLoadingInformation<TThirdPartyType, TOwnedType> dataLoadingInfo,
                Reconciliator<TThirdPartyType, TOwnedType> reconciliator,
                IMatcher matcher)
            where TThirdPartyType : ICSVRecord, new()
            where TOwnedType : ICSVRecord, new()
        {
            _inputOutput.OutputLine("Creating reconciliation interface...");
            var reconciliationInterface = new ReconciliationInterface<TThirdPartyType, TOwnedType>(
                new InputOutput(),
                reconciliator,
                dataLoadingInfo.ThirdPartyDescriptor,
                dataLoadingInfo.OwnedFileDescriptor,
                matcher);
            return reconciliationInterface;
        }

        public BudgetingMonths RecursivelyAskForBudgetingMonths(ISpreadsheet spreadsheet)
        {
            DateTime nextUnplannedMonth = GetNextUnplannedMonth(spreadsheet);
            int lastMonthForBudgetPlanning = GetLastMonthForBudgetPlanning(spreadsheet, nextUnplannedMonth.Month);
            var budgetingMonths = new BudgetingMonths
            {
                NextUnplannedMonth = nextUnplannedMonth.Month,
                LastMonthForBudgetPlanning = lastMonthForBudgetPlanning,
                StartYear = nextUnplannedMonth.Year
            };
            if (lastMonthForBudgetPlanning != 0)
            {
                budgetingMonths.LastMonthForBudgetPlanning = ConfirmBudgetingMonthChoicesWithUser(budgetingMonths, spreadsheet);
            }
            return budgetingMonths;
        }

        public DateTime GetNextUnplannedMonth(ISpreadsheet spreadsheet)
        {
            DateTime defaultMonth = DateTime.Today;
            DateTime nextUnplannedMonth = defaultMonth;
            bool badInput = false;
            try
            {
                nextUnplannedMonth = spreadsheet.GetNextUnplannedMonth();
            }
            catch (Exception)
            {
                string newMonth = _inputOutput.GetInput(ReconConsts.CantFindMortgageRow);
                try
                {
                    if (!String.IsNullOrEmpty(newMonth) && Char.IsDigit(newMonth[0]))
                    {
                        int actualMonth = Convert.ToInt32(newMonth);
                        if (actualMonth < 1 || actualMonth > 12)
                        {
                            badInput = true;
                        }
                        else
                        {
                            var year = defaultMonth.Year;
                            if (actualMonth < defaultMonth.Month)
                            {
                                year++;
                            }
                            nextUnplannedMonth = new DateTime(year, actualMonth, 1);
                        }
                    }
                    else
                    {
                        badInput = true;
                    }
                }
                catch (Exception)
                {
                    badInput = true;
                }
            }

            if (badInput)
            {
                _inputOutput.OutputLine(ReconConsts.DefaultUnplannedMonth);
                nextUnplannedMonth = defaultMonth;
            }

            return nextUnplannedMonth;
        }

        public int GetLastMonthForBudgetPlanning(ISpreadsheet spreadsheet, int nextUnplannedMonth)
        {
            string nextUnplannedMonthAsString = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(nextUnplannedMonth);
            var requestToEnterMonth = String.Format(ReconConsts.EnterMonths, nextUnplannedMonthAsString);
            string month = _inputOutput.GetInput(requestToEnterMonth);
            int result = 0;

            try
            {
                if (!String.IsNullOrEmpty(month) && Char.IsDigit(month[0]))
                {
                    result = Convert.ToInt32(month);
                    if (result < 1 || result > 12)
                    {
                        result = 0;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore it and return zero by default.
            }

            result = HandleZeroMonthChoiceResult(result, spreadsheet, nextUnplannedMonth);
            return result;
        }

        public int ConfirmBudgetingMonthChoicesWithUser(BudgetingMonths budgetingMonths, ISpreadsheet spreadsheet)
        {
            var newResult = budgetingMonths.LastMonthForBudgetPlanning;
            string input = GetResponseToBudgetingMonthsConfirmationMessage(budgetingMonths);

            if (!String.IsNullOrEmpty(input) && input.ToUpper() == "Y")
            {
                // I know this doesn't really do anything but I found the if statement easier to parse this way round.
                newResult = budgetingMonths.LastMonthForBudgetPlanning;
            }
            else
            {
                // Recursion ftw!
                newResult = GetLastMonthForBudgetPlanning(spreadsheet, budgetingMonths.NextUnplannedMonth);
            }

            return newResult;
        }

        public string GetResponseToBudgetingMonthsConfirmationMessage(BudgetingMonths budgetingMonths)
        {
            string firstMonth = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(budgetingMonths.NextUnplannedMonth);
            string secondMonth = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(budgetingMonths.LastMonthForBudgetPlanning);

            int monthSpan = budgetingMonths.NumBudgetingMonths();

            var confirmationText = String.Format(ReconConsts.ConfirmMonthInterval, firstMonth, secondMonth, monthSpan);

            return _inputOutput.GetInput(confirmationText);
        }

        public int HandleZeroMonthChoiceResult(int chosenMonth, ISpreadsheet spreadsheet, int nextUnplannedMonth)
        {
            var newResult = chosenMonth;
            if (chosenMonth == 0)
            {
                var input = _inputOutput.GetInput(ReconConsts.ConfirmBadMonth);

                if (!String.IsNullOrEmpty(input) && input.ToUpper() == "Y")
                {
                    newResult = 0;
                    _inputOutput.OutputLine(ReconConsts.ConfirmNoMonthlyBudgeting);
                }
                else
                {
                    // Recursion ftw!
                    newResult = GetLastMonthForBudgetPlanning(spreadsheet, nextUnplannedMonth);
                }
            }
            return newResult;
        }
    }
}