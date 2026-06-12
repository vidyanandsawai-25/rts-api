using NtisPlatform.Application.Interfaces;
using System;

namespace NtisPlatform.Application.Services
{
    public class SystemFinanceYearProvider : IFinanceYearProvider
    {
        // Indian fiscal year starts April 1.
        // April–December → current calendar year; January–March → previous calendar year.
        public int GetCurrentFinanceYear()
        {
            var today = DateTime.Today;
            return today.Month >= 4 ? today.Year : today.Year - 1;
        }
    }
}
