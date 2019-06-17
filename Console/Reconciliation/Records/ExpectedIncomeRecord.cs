using System;
using ConsoleCatchall.Console.Reconciliation.Extensions;
using Interfaces;
using Interfaces.Constants;
using Interfaces.DTOs;

namespace ConsoleCatchall.Console.Reconciliation.Records
{
    internal class ExpectedIncomeRecord : ICSVRecord
    {
        public ICSVRecord Match { get; set; }
        public bool Matched { get; set; }
        public bool Divider { get; set; }
        public string Source_line { get; private set; }

        // Source data - loaded on startup (if any new essential fields are added, update EssentialFields value below):
        public DateTime Date { get; set; }
        public double Unreconciled_amount { get; set; }
        public string Code { get; set; }
        public double Reconciled_amount { get; set; }
        public DateTime Date_paid { get; set; }
        public double Total_paid { get; set; }
        public string Description { get; set; }

        private char _separator = ',';

        public const int DateIndex = 0;
        public const int UnreconciledAmountIndex = 1;
        public const int DividerIndex = 1;
        public const int CodeIndex = 2;
        public const int ReconciledAmountIndex = 3;
        public const int DatePaidIndex = 4;
        public const int TotalPaidIndex = 5;
        public const int DescriptionIndex = 6;

        public const string EssentialFields = "unreconciled amount, code or description";

        public void Create_from_match(DateTime date, double amount, string type, string description, int extraInfo,
            ICSVRecord matchedRecord)
        {
            throw new NotImplementedException();
        }

        public void Load(string csvLine, char? overrideSeparator = null)
        {
            throw new NotImplementedException();
        }

        public bool Main_amount_is_negative()
        {
            return Unreconciled_amount < 0;
        }

        public void Make_main_amount_positive()
        {
            Unreconciled_amount = Math.Abs(Unreconciled_amount);
        }

        public void Swap_sign_of_main_amount()
        {
            Unreconciled_amount = Unreconciled_amount * -1;
        }

        public void Reconcile()
        {
            Reconciled_amount = Unreconciled_amount;
            Unreconciled_amount = 0;
        }

        public string To_csv(bool formatCurrency = true)
        {
            return To_string(',', true, formatCurrency);
        }

        public ConsoleLine To_console(int index = -1)
        {
            return new ConsoleLine
            {
                Index = index,
                Date_string = Date.ToString(@"dd\/MM\/yyyy"),
                Amount_string = Unreconciled_amount.To_csv_string(true),
                Description_string = Description
            };
        }

        public void Populate_spreadsheet_row(ICellSet cellSet, int rowNumber)
        {
            if (Divider)
            {
                cellSet.Populate_cell(rowNumber, DividerIndex + 1, ReconConsts.DividerText);
            }
            else
            {
                cellSet.Populate_cell(rowNumber, DateIndex + 1, Date);
                Populate_cell_with_amount(cellSet, rowNumber, UnreconciledAmountIndex, Unreconciled_amount);
                cellSet.Populate_cell(rowNumber, CodeIndex + 1, Code);
                Populate_cell_with_amount(cellSet, rowNumber, ReconciledAmountIndex, Reconciled_amount);
                cellSet.Populate_cell(rowNumber, DatePaidIndex + 1, Date_paid);
                Populate_cell_with_amount(cellSet, rowNumber, TotalPaidIndex, Total_paid);
                cellSet.Populate_cell(rowNumber, DescriptionIndex + 1, Description);
            }
        }

        private void Populate_cell_with_amount(ICellSet cellSet, int rowNumber, int amountIndex, double amount)
        {
            if (amount != 0)
            {
                cellSet.Populate_cell(rowNumber, amountIndex + 1, amount);
            }
            else
            {
                cellSet.Populate_cell(rowNumber, amountIndex + 1, String.Empty);
            }
        }

        public void Read_from_spreadsheet_row(ICellRow cellRow)
        {
            Date = DateTime.FromOADate(cellRow.Read_cell(DateIndex) != null 
                ? (double)cellRow.Read_cell(DateIndex)
                : 0);
            Unreconciled_amount = cellRow.Count > UnreconciledAmountIndex && cellRow.Read_cell(UnreconciledAmountIndex) != null
                ? (Double)cellRow.Read_cell(UnreconciledAmountIndex)
                : 0;
            Code = (String)cellRow.Read_cell(CodeIndex);
            Reconciled_amount = cellRow.Count > ReconciledAmountIndex && cellRow.Read_cell(ReconciledAmountIndex) != null
                ? (Double)cellRow.Read_cell(ReconciledAmountIndex)
                : 0;
            Date_paid = DateTime.FromOADate(cellRow.Read_cell(DatePaidIndex) != null
                ? (double)cellRow.Read_cell(DatePaidIndex)
                : 0);
            Total_paid = cellRow.Count > TotalPaidIndex && cellRow.Read_cell(TotalPaidIndex) != null
                ? (Double)cellRow.Read_cell(TotalPaidIndex)
                : 0;
            Description = (String)cellRow.Read_cell(DescriptionIndex);

            Source_line = To_string(_separator, false);
        }

        private String To_string(char separator, bool encaseDescriptionInQuotes = true, bool formatCurrency = true)
        {
            return (Date.ToOADate() == 0 ? "" : Date.ToString(@"dd\/MM\/yyyy")) + separator
                   + (Unreconciled_amount == 0 ? "" : Unreconciled_amount.To_csv_string(formatCurrency)) + separator
                   + Code + separator
                   + (Reconciled_amount == 0 ? "" : Reconciled_amount.To_csv_string(formatCurrency)) + separator
                   + (Date_paid.ToOADate() == 0 ? "" : Date_paid.ToString(@"dd\/MM\/yyyy")) + separator
                   + (Total_paid == 0 ? "" : Total_paid.To_csv_string(formatCurrency)) + separator
                   + (string.IsNullOrEmpty(Description)
                        ? ""
                        : (encaseDescriptionInQuotes ? Description.Encase_in_escaped_quotes_if_not_already_encased() : Description));
        }

        public void Convert_source_line_separators(char originalSeparator, char newSeparator)
        {
            throw new NotImplementedException();
        }

        public double Main_amount()
        {
            return Unreconciled_amount;
        }

        public void Change_main_amount(double newValue)
        {
            Unreconciled_amount = newValue;
        }

        public string Transaction_type()
        {
            return Code;
        }

        public int Extra_info()
        {
            return 0;
        }

        public ICSVRecord Copy()
        {
            return new ExpectedIncomeRecord
            {
                Date = Date,
                Unreconciled_amount = Unreconciled_amount,
                Code = Code,
                Reconciled_amount = Reconciled_amount,
                Date_paid = Date_paid,
                Total_paid = Total_paid,
                Description = Description,
                Source_line = Source_line
            };
        }

        public ICSVRecord With_date(DateTime newDate)
        {
            Date = newDate;
            return this;
        }

        public void Update_source_line_for_output(char outputSeparator)
        {
            Source_line = To_string(outputSeparator);
        }

        public BankRecord Convert_to_bank_record()
        {
            return new BankRecord
            {
                Date = Date,
                Unreconciled_amount = Unreconciled_amount,
                Type = Code,
                Description = Description
            };
        }
    }
}