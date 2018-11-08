using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using System.Threading.Tasks;

namespace Csv2Class.Standard
{
    public static class ClassGenerator
    {
        public static string ToClass(this Func<Stream> csvStreamFactory, 
            bool hasHeader = true, string className = "Model")
        {
            using(var stream = csvStreamFactory())
            {
                return stream.ToClass(hasHeader, className);
            }
        }

        public async static Task<string> ToClass(this Task<Stream> csvStreamTask, 
            bool hasHeader = true, string className = "Model")
        {
            using(var stream = await csvStreamTask)
            {
                return stream.ToClass(hasHeader, className);
            }
        }

        private static Regex rgxStartWithDigit = new Regex(@"^\d");

        private static void BadDataFound(ReadingContext context)
        {
            int row = context.Row;
            long position = context.BytePosition;
            Debugger.Break();
        }

        public static string ToClass(this Stream stream, 
            bool hasHeader = true, string className = "Model")
        {
            using(StreamReader csvStreamReader = new StreamReader(stream))
            {
                Configuration configuration = new Configuration()
                {
                    IgnoreQuotes = true,
                    BadDataFound = BadDataFound
                };
                using(CsvReader csvReader = new CsvReader(csvStreamReader, configuration))
                {


                    // possibilities, string, int, double, DateTime, that's all

                    List<FieldOpportunity> fields = null;
                    if (hasHeader)
                    {
                        fields = new List<FieldOpportunity>();
                        csvReader.Read(); //skip header line
                        csvReader
                            .ReadAllColumns()
                            .ForEach(header =>
                        {
                            if (rgxStartWithDigit.IsMatch(header))
                            {
                                header = $"_{header}";
                            }
                            fields.Add(new FieldOpportunity()
                            {
                                Name = header.Replace(" ", "_")
                            });
                        });
                    }

                    while (csvReader.Read())
                    {
                        var row = csvReader.ReadAllColumns();
                        if (fields == null)
                        {
                            fields = new List<FieldOpportunity>();
                            var length = row.Count.ToString().Length;
                            int i = 0;
                            row.ForEach((item) =>
                            {
                                fields.Add(new FieldOpportunity()
                                {
                                    Name = $"Column{i.ToString().PadLeft(length, '0')}"
                                });
                                i += 1;
                            });
                        }

                        for(int i = 0; i < Math.Min(row.Count,fields.Count); i++)
                        {
                            var field = fields[i];
                            field.Initialized = true;
                            // try parse
                            if (field.CanConvertToDateTime)
                            {
                                DateTime dateTimeValue = DateTime.Now;
                                field.CanConvertToDateTime = DateTime.TryParse(row[i], out dateTimeValue);
                            }
                            if (field.CanConvertToDouble)
                            {
                                double doubleValue = 0d;
                                field.CanConvertToDouble = double.TryParse(row[i], out doubleValue);
                            }
                            if (field.CanCnovertToInt)
                            {
                                int intValue = 0;
                                field.CanCnovertToInt = int.TryParse(row[i], out intValue);
                            }
                        }
                    }
                    // code generator

                    List<string> properties = fields
                        .Select(f =>
                        {
                            if (f.CanConvertToDateTime)
                            {
                                return $"\tpublic {nameof(DateTime)} {f.Name} {{ get; set; }}\n";
                            }
                            if (f.CanCnovertToInt)
                            {
                                return $"\tpublic int {f.Name} {{ get; set; }}\n";
                            }
                            if (f.CanConvertToDouble)
                            {
                                return $"\tpublic double {f.Name} {{ get; set; }}\n";
                            }
                            return $"\tpublic string {f.Name} {{ get; set; }}\n";
                        })
                        .ToList();
                    return $@"public class {className}
{{
{string.Join("", properties)}
}}";
                }
               
            }
        }

        
        private static List<string> ReadAllColumns(this CsvReader csvReader)
        {
            List<string> data = new List<string>();
            bool shouldTryMoreColumn = true;
            int i = 0;
            string columnValue;
            while (shouldTryMoreColumn)
            {
                shouldTryMoreColumn = csvReader.TryGetField(i, out columnValue);
                if (shouldTryMoreColumn)
                {
                    data.Add(columnValue);
                    i += 1;
                }
            }
            return data;
        }
    }

    public class FieldOpportunity
    {
        public string Name { get; set; }
        public bool CanConvertToString { get; set; } = true;
        public bool CanCnovertToInt { get; set; } = true;
        public bool CanConvertToDouble { get; set; } = true;
        public bool CanConvertToDateTime { get; set; } = true;
        public bool Initialized { get; set; } = false;
    }
}
