﻿//==============================================================
// Forex Strategy Builder
// Copyright © Miroslav Popov. All rights reserved.
//==============================================================
// THIS CODE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE.
//==============================================================

using System;
using System.Drawing;
using ForexStrategyBuilder.Infrastructure.Entities;
using ForexStrategyBuilder.Infrastructure.Enums;
using ForexStrategyBuilder.Infrastructure.Interfaces;

namespace ForexStrategyBuilder.Indicators.Store
{
    public class AroonHistogram : Indicator
    {
        public AroonHistogram()
        {
            IndicatorName = "Aroon Histogram";
            PossibleSlots = SlotTypes.OpenFilter | SlotTypes.CloseFilter;
            SeparatedChart = true;
            SeparatedChartMinValue = -100;
            SeparatedChartMaxValue = 100;
            IsDiscreteValues = true; // <------------

            IndicatorAuthor = "Miroslav Popov";
            IndicatorVersion = "2.0";
            IndicatorDescription = "Bundled in FSB distribution.";
        }

        public override void Initialize(SlotTypes slotType)
        {
            SlotType = slotType;

            // The ComboBox parameters
            IndParam.ListParam[0].Caption = "Logic";
            IndParam.ListParam[0].ItemList = new[]
                {
                    "Aroon Histogram rises",
                    "Aroon Histogram falls",
                    "Aroon Histogram is higher than the Level line",
                    "Aroon Histogram is lower than the Level line",
                    "Aroon Histogram crosses the Level line upward",
                    "Aroon Histogram crosses the Level line downward",
                    "Aroon Histogram changes its direction upward",
                    "Aroon Histogram changes its direction downward"
                };
            IndParam.ListParam[0].Index = 0;
            IndParam.ListParam[0].Text = IndParam.ListParam[0].ItemList[IndParam.ListParam[0].Index];
            IndParam.ListParam[0].Enabled = true;
            IndParam.ListParam[0].ToolTip = "Logic of application of the indicator.";

            IndParam.ListParam[1].Caption = "Base price";
            IndParam.ListParam[1].ItemList = Enum.GetNames(typeof (BasePrice));
            IndParam.ListParam[1].Index = (int) BasePrice.Close;
            IndParam.ListParam[1].Text = IndParam.ListParam[1].ItemList[IndParam.ListParam[1].Index];
            IndParam.ListParam[1].Enabled = true;
            IndParam.ListParam[1].ToolTip = "The price the Aroon is based on.";

            // The NumericUpDown parameters
            IndParam.NumParam[0].Caption = "Period";
            IndParam.NumParam[0].Value = 9;
            IndParam.NumParam[0].Min = 1;
            IndParam.NumParam[0].Max = 200;
            IndParam.NumParam[0].Enabled = true;
            IndParam.NumParam[0].ToolTip = "Period used to calculate the Aroon value.";

            IndParam.NumParam[1].Caption = "Level";
            IndParam.NumParam[1].Value = 0;
            IndParam.NumParam[1].Min = -100;
            IndParam.NumParam[1].Max = 100;
            IndParam.NumParam[1].Enabled = true;
            IndParam.NumParam[1].ToolTip = "A critical level (for the appropriate logic).";

            // The CheckBox parameters
            IndParam.CheckParam[0].Caption = "Use previous bar value";
            IndParam.CheckParam[0].Enabled = true;
            IndParam.CheckParam[0].ToolTip = "Use the indicator value from the previous bar.";
        }

        public override void Calculate(IDataSet dataSet)
        {
            DataSet = dataSet;

            // Reading the parameters
            var basePrice = (BasePrice) IndParam.ListParam[1].Index;
            var iPeriod = (int) IndParam.NumParam[0].Value;
            double dLevel = IndParam.NumParam[1].Value;
            int iPrvs = IndParam.CheckParam[0].Checked ? 1 : 0;

            // Calculation
            int iFirstBar = iPeriod + 2;
            double[] adBasePrice = Price(basePrice);
            var adUp = new double[Bars];
            var adDown = new double[Bars];
            var adAroon = new double[Bars];

            for (int iBar = iPeriod; iBar < Bars; iBar++)
            {
                double dHighestHigh = double.MinValue;
                double dLowestLow = double.MaxValue;
                for (int i = 0; i < iPeriod; i++)
                {
                    int iBaseBar = iBar - iPeriod + 1 + i;
                    if (adBasePrice[iBaseBar] > dHighestHigh)
                    {
                        dHighestHigh = adBasePrice[iBaseBar];
                        adUp[iBar] = 100.0*i/(iPeriod - 1);
                    }
                    if (adBasePrice[iBaseBar] < dLowestLow)
                    {
                        dLowestLow = adBasePrice[iBaseBar];
                        adDown[iBar] = 100.0*i/(iPeriod - 1);
                    }
                }
            }

            for (int iBar = iFirstBar; iBar < Bars; iBar++)
            {
                adAroon[iBar] = adUp[iBar] - adDown[iBar];
            }

            // Saving the components
            Component = new IndicatorComp[5];

            Component[0] = new IndicatorComp
                {
                    CompName = "Aroon Histogram",
                    DataType = IndComponentType.IndicatorValue,
                    ChartType = IndChartType.Histogram,
                    FirstBar = iFirstBar,
                    Value = adAroon
                };

            Component[1] = new IndicatorComp
                {
                    CompName = "Aroon Up",
                    DataType = IndComponentType.IndicatorValue,
                    ChartType = IndChartType.Line,
                    ChartColor = Color.Green,
                    FirstBar = iFirstBar,
                    Value = adUp
                };

            Component[2] = new IndicatorComp
                {
                    CompName = "Aroon Down",
                    DataType = IndComponentType.IndicatorValue,
                    ChartType = IndChartType.Line,
                    ChartColor = Color.Red,
                    FirstBar = iFirstBar,
                    Value = adDown
                };

            Component[3] = new IndicatorComp
                {
                    ChartType = IndChartType.NoChart,
                    FirstBar = iFirstBar,
                    Value = new double[Bars]
                };

            Component[4] = new IndicatorComp
                {
                    ChartType = IndChartType.NoChart,
                    FirstBar = iFirstBar,
                    Value = new double[Bars]
                };

            // Sets the Component's type
            if (SlotType == SlotTypes.OpenFilter)
            {
                Component[3].DataType = IndComponentType.AllowOpenLong;
                Component[3].CompName = "Is long entry allowed";
                Component[4].DataType = IndComponentType.AllowOpenShort;
                Component[4].CompName = "Is short entry allowed";
            }
            else if (SlotType == SlotTypes.CloseFilter)
            {
                Component[3].DataType = IndComponentType.ForceCloseLong;
                Component[3].CompName = "Close out long position";
                Component[4].DataType = IndComponentType.ForceCloseShort;
                Component[4].CompName = "Close out short position";
            }

            // Calculation of the logic
            var indLogic = IndicatorLogic.It_does_not_act_as_a_filter;

            switch (IndParam.ListParam[0].Text)
            {
                case "Aroon Histogram rises":
                    indLogic = IndicatorLogic.The_indicator_rises;
                    SpecialValues = new double[] {0};
                    break;

                case "Aroon Histogram falls":
                    indLogic = IndicatorLogic.The_indicator_falls;
                    SpecialValues = new double[] {0};
                    break;

                case "Aroon Histogram is higher than the Level line":
                    indLogic = IndicatorLogic.The_indicator_is_higher_than_the_level_line;
                    SpecialValues = new[] {dLevel, -dLevel};
                    break;

                case "Aroon Histogram is lower than the Level line":
                    indLogic = IndicatorLogic.The_indicator_is_lower_than_the_level_line;
                    SpecialValues = new[] {dLevel, -dLevel};
                    break;

                case "Aroon Histogram crosses the Level line upward":
                    indLogic = IndicatorLogic.The_indicator_crosses_the_level_line_upward;
                    SpecialValues = new[] {dLevel, -dLevel};
                    break;

                case "Aroon Histogram crosses the Level line downward":
                    indLogic = IndicatorLogic.The_indicator_crosses_the_level_line_downward;
                    SpecialValues = new[] {dLevel, -dLevel};
                    break;

                case "Aroon Histogram changes its direction upward":
                    indLogic = IndicatorLogic.The_indicator_changes_its_direction_upward;
                    SpecialValues = new double[] {0};
                    break;

                case "Aroon Histogram changes its direction downward":
                    indLogic = IndicatorLogic.The_indicator_changes_its_direction_downward;
                    SpecialValues = new double[] {0};
                    break;
            }

            OscillatorLogic(iFirstBar, iPrvs, adAroon, dLevel, -dLevel, ref Component[3], ref Component[4], indLogic);
        }

        public override void SetDescription()
        {
            bool isLevelZero = Math.Abs(IndParam.NumParam[1].Value - 0) < Epsilon;
            string levelLong = (isLevelZero ? "0" : IndParam.NumParam[1].ValueToString);
            string levelShort = (isLevelZero ? "0" : "-" + levelLong);

            EntryFilterLongDescription = "the " + ToString() + " ";
            EntryFilterShortDescription = "the " + ToString() + " ";
            ExitFilterLongDescription = "the " + ToString() + " ";
            ExitFilterShortDescription = "the " + ToString() + " ";

            switch (IndParam.ListParam[0].Text)
            {
                case "Aroon Histogram rises":
                    EntryFilterLongDescription += "rises";
                    EntryFilterShortDescription += "falls";
                    ExitFilterLongDescription += "rises";
                    ExitFilterShortDescription += "falls";
                    break;

                case "Aroon Histogram falls":
                    EntryFilterLongDescription += "falls";
                    EntryFilterShortDescription += "rises";
                    ExitFilterLongDescription += "falls";
                    ExitFilterShortDescription += "rises";
                    break;

                case "Aroon Histogram is higher than the Level line":
                    EntryFilterLongDescription += "is higher than the Level " + levelLong;
                    EntryFilterShortDescription += "is lower than the Level " + levelShort;
                    ExitFilterLongDescription += "is higher than the Level " + levelLong;
                    ExitFilterShortDescription += "is lower than the Level " + levelShort;
                    break;

                case "Aroon Histogram is lower than the Level line":
                    EntryFilterLongDescription += "is lower than the Level " + levelLong;
                    EntryFilterShortDescription += "is higher than the Level " + levelShort;
                    ExitFilterLongDescription += "is lower than the Level " + levelLong;
                    ExitFilterShortDescription += "is higher than the Level " + levelShort;
                    break;

                case "Aroon Histogram crosses the Level line upward":
                    EntryFilterLongDescription += "crosses the Level " + levelLong + " upward";
                    EntryFilterShortDescription += "crosses the Level " + levelShort + " downward";
                    ExitFilterLongDescription += "crosses the Level " + levelLong + " upward";
                    ExitFilterShortDescription += "crosses the Level " + levelShort + " downward";
                    break;

                case "Aroon Histogram crosses the Level line downward":
                    EntryFilterLongDescription += "crosses the Level " + levelLong + " downward";
                    EntryFilterShortDescription += "crosses the Level " + levelShort + " upward";
                    ExitFilterLongDescription += "crosses the Level " + levelLong + " downward";
                    ExitFilterShortDescription += "crosses the Level " + levelShort + " upward";
                    break;

                case "Aroon Histogram changes its direction upward":
                    EntryFilterLongDescription += "changes its direction upward";
                    EntryFilterShortDescription += "changes its direction downward";
                    ExitFilterLongDescription += "changes its direction upward";
                    ExitFilterShortDescription += "changes its direction downward";
                    break;

                case "Aroon Histogram changes its direction downward":
                    EntryFilterLongDescription += "changes its direction downward";
                    EntryFilterShortDescription += "changes its direction upward";
                    ExitFilterLongDescription += "changes its direction downward";
                    ExitFilterShortDescription += "changes its direction upward";
                    break;
            }
        }

        public override string ToString()
        {
            return IndicatorName +
                   (IndParam.CheckParam[0].Checked ? "* (" : " (") +
                   IndParam.ListParam[1].Text + ", " + // Base price
                   IndParam.NumParam[0].ValueToString + ")"; // Smoothing period
        }
    }
}