using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using Csv2Class.Standard;

namespace Csv2Class.Tests
{
    public class UnitTest1
    {
        [Theory(DisplayName = "Convert a CSV File")]
        [InlineData(@"C:\Users\zhenyu.shi\Documents\Geo Dashboard\ETL Samples\DimStore_20181107.csv")]
        public void ConvertCsvFile(string filename)
        {

            Func<Stream> fac = () => File.OpenRead(filename);
            var code = fac.ToClass(true, "DimStoreEtl");

            Debugger.Break();
        }
    }
}
