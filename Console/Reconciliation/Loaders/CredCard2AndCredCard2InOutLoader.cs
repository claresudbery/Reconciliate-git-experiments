﻿using System;
using System.Collections.Generic;
using ConsoleCatchall.Console.Reconciliation.Files;
using ConsoleCatchall.Console.Reconciliation.Reconciliators;
using ConsoleCatchall.Console.Reconciliation.Records;
using ConsoleCatchall.Console.Reconciliation.Spreadsheets;
using Interfaces;
using Interfaces.Constants;
using Interfaces.DTOs;

namespace ConsoleCatchall.Console.Reconciliation.Loaders
{
    internal class CredCard2AndCredCard2InOutLoader
    {
        private readonly IInputOutput _input_output;
        private readonly ISpreadsheetRepoFactory _spreadsheet_factory;

        public CredCard2AndCredCard2InOutLoader(IInputOutput input_output, ISpreadsheetRepoFactory spreadsheet_factory)
        {
            _input_output = input_output;
            _spreadsheet_factory = spreadsheet_factory;
        }

        public ReconciliationInterface
            Load(ISpreadsheet spreadsheet,
                BudgetingMonths budgeting_months,
                FilePaths main_file_paths,
                IFileIO<CredCard2InOutRecord> pending_file_io,
                ICSVFile<CredCard2InOutRecord> pending_file,
                IFileIO<CredCard2Record> third_party_file_io,
                IFileIO<CredCard2InOutRecord> owned_file_io)
        {
            var data_loading_info = CredCard2AndCredCard2InOutData.LoadingInfo;
            data_loading_info.File_paths = main_file_paths;

            Load_pending_data(pending_file_io, pending_file, data_loading_info);
            Merge_budget_data(spreadsheet, budgeting_months, pending_file, data_loading_info);
            Merge_other_data(spreadsheet, budgeting_months, pending_file, data_loading_info);
            Merge_unreconciled_data(spreadsheet, pending_file, data_loading_info);
            var reconciliator = Load_third_party_and_owned_files_into_reconciliator_reconciliator(third_party_file_io, owned_file_io, data_loading_info);
            return Create_reconciliation_interface(reconciliator, data_loading_info);
        }

        private ReconciliationInterface Create_reconciliation_interface(CredCard2Reconciliator reconciliator, DataLoadingInformation data_loading_info)
        {
            _input_output.Output_line(ReconConsts.CreatingReconciliationInterface);
            return new ReconciliationInterface(
                _input_output,
                reconciliator,
                data_loading_info.Third_party_descriptor,
                data_loading_info.Owned_file_descriptor);
        }

        private CredCard2Reconciliator Load_third_party_and_owned_files_into_reconciliator_reconciliator(
            IFileIO<CredCard2Record> third_party_file_io, IFileIO<CredCard2InOutRecord> owned_file_io, DataLoadingInformation data_loading_info)
        {
            _input_output.Output_line(ReconConsts.LoadingDataFromFiles);
            third_party_file_io.Set_file_paths(data_loading_info.File_paths.Main_path,
                data_loading_info.File_paths.Third_party_file_name);
            owned_file_io.Set_file_paths(data_loading_info.File_paths.Main_path, data_loading_info.File_paths.Owned_file_name);
            var reconciliator = new CredCard2Reconciliator(third_party_file_io, owned_file_io);
            return reconciliator;
        }

        private void Merge_unreconciled_data(ISpreadsheet spreadsheet, ICSVFile<CredCard2InOutRecord> pending_file,
            DataLoadingInformation data_loading_info)
        {
// Pending file will already exist, having already been split out from phone Notes file by a separate function call.
            // We loaded it up into memory in the previous file-specific method.
            // Then some budget amounts were added to that file (in memory).
            // Other budget amounts (like CredCard1 balance) were written directly to the spreadsheet before this too.
            // Now we load the unreconciled rows from the spreadsheet and merge them with the pending and budget data.
            // Then we write all that data away into the 'owned' csv file (eg BankOutPending.csv).
            _input_output.Output_line(ReconConsts.MergingUnreconciledRows);
            spreadsheet.Add_unreconciled_rows_to_csv_file(data_loading_info.Sheet_name, pending_file);
            _input_output.Output_line(ReconConsts.CopyingMergedData);
            pending_file.Update_source_lines_for_output(data_loading_info.Loading_separator);
            pending_file.Write_to_file_as_source_lines(data_loading_info.File_paths.Owned_file_name);
            _input_output.Output_line(ReconConsts.StuffIsHappening);
        }

        private void Merge_other_data(ISpreadsheet spreadsheet, BudgetingMonths budgeting_months, ICSVFile<CredCard2InOutRecord> pending_file,
            DataLoadingInformation data_loading_info)
        {
            _input_output.Output_line(ReconConsts.MergingBespokeData);
            Merge_bespoke_data_with_pending_file(
                _input_output,
                spreadsheet,
                pending_file,
                budgeting_months,
                data_loading_info);
        }

        private void Merge_budget_data(ISpreadsheet spreadsheet, BudgetingMonths budgeting_months, ICSVFile<CredCard2InOutRecord> pending_file,
            DataLoadingInformation data_loading_info)
        {
            _input_output.Output_line(ReconConsts.MergingBudgetDataWithPendingData);
            spreadsheet.Add_budgeted_cred_card2_in_out_data_to_pending_file(
                budgeting_months,
                pending_file,
                data_loading_info.Monthly_budget_data);
        }

        private void Load_pending_data(IFileIO<CredCard2InOutRecord> pending_file_io, ICSVFile<CredCard2InOutRecord> pending_file, DataLoadingInformation data_loading_info)
        {
            _input_output.Output_line(ReconConsts.LoadingDataFromPendingFile);
            pending_file_io.Set_file_paths(data_loading_info.File_paths.Main_path, data_loading_info.Pending_file_name);
            pending_file.Load(true, data_loading_info.Default_separator);
            // The separator we loaded with had to match the source. Then we convert it here to match its destination.
            _input_output.Output_line(ReconConsts.ConvertingSourceLineSeparators);
            pending_file.Convert_source_line_separators(data_loading_info.Default_separator,
                data_loading_info.Loading_separator);
        }

        public void Merge_bespoke_data_with_pending_file(
            IInputOutput input_output,
            ISpreadsheet spreadsheet,
            ICSVFile<CredCard2InOutRecord> pending_file,
            BudgetingMonths budgeting_months,
            DataLoadingInformation loading_info)
        {
            var most_recent_cred_card_direct_debit = spreadsheet.Get_most_recent_row_containing_text<BankRecord>(
                MainSheetNames.Bank_out,
                ReconConsts.Cred_card2_dd_description,
                new List<int> { ReconConsts.DescriptionColumn, ReconConsts.DdDescriptionColumn });

            var statement_date = new DateTime();
            var next_date = most_recent_cred_card_direct_debit.Date.AddMonths(1);
            var input = input_output.Get_input(string.Format(
                ReconConsts.AskForCredCardDirectDebit,
                ReconConsts.Cred_card2_name,
                next_date.ToShortDateString()));
            double new_balance = 0;
            while (input != "0")
            {
                if (double.TryParse(input, out new_balance))
                {
                    pending_file.Records.Add(new CredCard2InOutRecord
                    {
                        Date = next_date,
                        Description = ReconConsts.Cred_card2_regular_pymt_description,
                        Unreconciled_amount = new_balance
                    });
                }
                statement_date = next_date.AddMonths(-1);
                next_date = next_date.Date.AddMonths(1);
                input = input_output.Get_input(string.Format(
                    ReconConsts.AskForCredCardDirectDebit,
                    ReconConsts.Cred_card2_name,
                    next_date.ToShortDateString()));
            }

            spreadsheet.Update_balance_on_totals_sheet(
                Codes.Cred_card2_bal,
                new_balance * -1,
                string.Format(
                    ReconConsts.CredCardBalanceDescription,
                    ReconConsts.Cred_card2_name,
                    $"{statement_date.ToString("MMM")} {statement_date.Year}"),
                balance_column: 5,
                text_column: 6,
                code_column: 4);
        }
    }
}