﻿using Interfaces.Constants;
using Interfaces.DTOs;

namespace ConsoleCatchall.Console.Reconciliation.Loaders
{
    internal static class BankAndBankInData
    {
        public static DataLoadingInformation LoadingInfo =
            new DataLoadingInformation
            {
                FilePaths = new FilePaths
                {
                    MainPath = ReconConsts.DefaultFilePath,
                    ThirdPartyFileName = ReconConsts.DefaultBankFileName,
                    OwnedFileName = ReconConsts.DefaultBankInFileName
                },
                DefaultSeparator = ',',
                LoadingSeparator = '^',
                PendingFileName = ReconConsts.DefaultBankInPendingFileName,
                SheetName = MainSheetNames.BankIn,
                ThirdPartyDescriptor = ReconConsts.BankDescriptor,
                OwnedFileDescriptor = ReconConsts.BankInDescriptor,
                MonthlyBudgetData = new BudgetItemListData
                {
                    SheetName = MainSheetNames.BudgetIn,
                    StartDivider = Dividers.Date,
                    EndDivider = Dividers.Total,
                    FirstColumnNumber = 2,
                    LastColumnNumber = 6
                },
                AnnualBudgetData = null,
                ThirdPartyFileLoadAction = ThirdPartyFileLoadAction.FilterForPositiveRecordsOnly
            };
    }
}