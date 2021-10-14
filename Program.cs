using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;

namespace Practice
{
    //main class develop version + bomba
    class Program
    {
        private static string a = "a_example.txt";
        private static string b = "b_read_on.txt";
        private static string c = "c_incunabula.txt";
        private static string d = "d_tough_choices.txt";
        private static string e = "e_so_many_books.txt";
        private static string f = "f_libraries_of_the_world.txt";

        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            ProcessFile(a, "a_result.txt");
            ProcessFile(b, "b_result.txt");
            ProcessFile(e, "e_result.txt");
            ProcessFile(f, "f_result.txt");
            ProcessFile(c, "c_result.txt");
            ProcessFile(d, "d_result.txt");
            sw.Stop();

            Console.WriteLine("Time: " + sw.Elapsed.TotalSeconds + " s");
            Console.ReadLine();
        }

        static void ProcessFile(string fileName, string outPut)
        {
            #region readdata
            var lines = File.ReadAllLines(fileName);
            var totalBooks = lines[0].Split(' ').Select(int.Parse).ElementAt(0);
            var totalLibraries = lines[0].Split(' ').Select(int.Parse).ElementAt(1);
            var totalDays = lines[0].Split(' ').Select(int.Parse).ElementAt(2);
            var bookScores = lines[1].Split(' ').Select(int.Parse).ToList();
            var libraries = new List<Library>();

            for (int i = 0; i <= totalLibraries - 1; i++)
            {
                var libBooksCount = lines[i * 2 + 2].Split(' ').Select(int.Parse).ElementAt(0);
                var signupDays = lines[i * 2 + 2].Split(' ').Select(int.Parse).ElementAt(1);
                var booksPerDay = lines[i * 2 + 2].Split(' ').Select(int.Parse).ElementAt(2);
                var libBooksId = lines[i * 2 + 3].Split(' ').Select(int.Parse).ToList();
                var lst = new List<Book>();

                foreach (var bkid in libBooksId)
                {
                    lst.Add(new Book() { Id = bkid, Score = bookScores[bkid] });
                }

                var libraryBooks = libBooksId.Select(b => bookScores[b]).ToList();

                libraries.Add(new Library
                {
                    Id = i,
                    BooksCount = libBooksCount,
                    SingupDays = signupDays,
                    BooksPerDay = booksPerDay,
                    Books = lst.OrderByDescending(b => b.Score).ToList(),
                    Rate = 0 // signupDays / booksPerDay * lst.Average(b=>b.Score)  + totalDays - signupDays + Convert.ToInt32(lst.Take(booksPerDay).Average(b=>b.Score) * (totalDays / totalBooks))
                });
            }

            Dictionary<int, int> useles = new Dictionary<int, int>();
            var maxSignup = libraries.Max(l => l.SingupDays);

            foreach (var l in libraries)
            {
                l.Rate = Math.Pow(maxSignup - l.SingupDays, 2.15);
                var ind = libraries.IndexOf(l);
                var dayspast = 0;
                if (ind > 0)
                {
                    for (int i = 0; i < ind; i++)
                    {
                        dayspast = dayspast + libraries[i].SingupDays;
                    }
                }
                else
                {
                    dayspast = l.SingupDays;
                }

                foreach (var b in l.Books)
                {
                    if (!useles.ContainsKey(b.Id))
                    {
                        useles.Add(b.Id, 1);
                    }
                    else
                    {
                        useles[b.Id] = useles[b.Id] + 1;
                    }
                }
            }

            foreach (var l in libraries)
            {
                foreach (var b in l.Books)
                {
                    b.UseCount = useles[b.Id];
                }
            }

            libraries.ForEach(l =>
            {
                var workingDays = totalDays - l.SingupDays;
                var libBooks = l.Books
                    .OrderByDescending(book => book.Score)
                    .ToList();

                var maxBooksForProcess = workingDays * l.BooksPerDay;
                var numberOfNewBooks = libBooks.Take(maxBooksForProcess).Count();
                var rateSum = libBooks.Take(numberOfNewBooks).Sum(cb => cb.Score);
                l.Rate = rateSum / l.SingupDays;
            });
            libraries = libraries.OrderByDescending(l => l.Rate).ToList();

            #endregion

            Library signedUpLib = null;
            List<Library> workingLibs = new List<Library>();
            int day = 0;
            HashSet<int> usedBooksId = new HashSet<int>();
            HashSet<int> signedUpBooksId = new HashSet<int>();
            HashSet<int> bookedbooksId = new HashSet<int>();

            while (day < totalDays)
            {
                int totalDaysLocal = totalDays;
                if (signedUpLib == null || signedUpLib.SignedUp)
                {
                    libraries.ForEach(l =>
                    {
                        var workingDays = totalDaysLocal - day - l.SingupDays;
                        if (workingDays <= 0)
                        {
                            l.Rate = -1;
                        }
                        else
                        {
                            var libBooks = l.Books
                                .Where(book => !usedBooksId.Contains(book.Id) && !bookedbooksId.Contains(book.Id))
                                .OrderByDescending(book => book.Score).ThenByDescending(bb => bb.UseCount)
                                .ToList();
                            long maxBooksForProcessLong = workingDays * l.BooksPerDay;
                            int maxBooksForProcess = 0;
                            if (maxBooksForProcessLong >= int.MaxValue || maxBooksForProcessLong < 0)
                            {
                                maxBooksForProcess = libBooks.Count;
                            }
                            else
                            {
                                maxBooksForProcess = (int)maxBooksForProcessLong;
                            }

                            var numberOfNewBooks = libBooks.Take(maxBooksForProcess).Count();
                            var rateSum = libBooks.Take(maxBooksForProcess).Sum(cb => cb.Score);
                            var idleTime = 0;
                            if (l.BooksPerDay > numberOfNewBooks)
                                idleTime = workingDays - 1;
                            else idleTime = (workingDays - (numberOfNewBooks / l.BooksPerDay));

                            if (idleTime == 0)
                            {
                                idleTime = 1;
                            }

                            if (rateSum == 0)
                            {
                                l.Rate = -1;
                            }
                            else
                            {
                                l.Rate = rateSum / (l.SingupDays);
                                if (l.Rate == 0)
                                {
                                    l.Rate = l.BooksForBooking.Count();
                                }
                                l.Rate2 = rateSum;// / idleTime;

                                l.BooksForBooking = libBooks.Take(numberOfNewBooks).Select(bk => bk.Id);
                            }
                        }
                    });
                    libraries = libraries.OrderByDescending(l => l.Rate).ThenByDescending(l => l.Rate2).ToList();
                    signedUpLib = libraries.FirstOrDefault();

                    foreach (var bkId in signedUpLib.Books.Select(b => b.Id))
                    {
                        signedUpBooksId.Add(bkId);
                    }

                    foreach (var bkId in signedUpLib.BooksForBooking)
                    {
                        bookedbooksId.Add(bkId);
                    }
                }

                foreach (var workingLib in workingLibs.Where(l => !l.Idle).OrderByDescending(l => l.Books.Count))
                {
                    var booksToScan = workingLib.Books
                        .Where(bk => !usedBooksId.Contains(bk.Id) && !signedUpBooksId.Contains(bk.Id))
                        .OrderByDescending(bb => bb.Score).ThenByDescending(bb => bb.UseCount)
                        .Take(workingLib.BooksPerDay)
                        .ToList();

                    if (booksToScan.Any())
                    {
                        workingLib.ProcessedBooksIds.AddRange(booksToScan.Select(b => b.Id));
                        foreach (var bId in booksToScan)
                        {
                            usedBooksId.Add(bId.Id);
                        }
                    }
                    else
                    {
                        workingLib.Idle = true;
                    }

                    if (usedBooksId.Count >= totalBooks)
                    {
                        break;
                    }
                }

                if (signedUpLib != null && !signedUpLib.SignedUp)
                {
                    signedUpLib.SingupDays--;
                    if (signedUpLib.SingupDays <= 0)
                    {
                        signedUpLib.SignedUp = true;
                        workingLibs.Add(signedUpLib);
                        var index = libraries.IndexOf(signedUpLib);
                        libraries.RemoveAt(index);
                        signedUpBooksId = new HashSet<int>();
                    }
                }

                if (usedBooksId.Count >= totalBooks && libraries.Count == 0)
                {
                    break;
                }

                day++;
            }

            var redundant = workingLibs.Where(l => l.ProcessedBooksIds.Count != l.BooksForBooking.Count()).ToList();

            var resultLibs = workingLibs.Where(l => l.ProcessedBooksIds.Any()).ToList();
            var booksIds = resultLibs.SelectMany(l => l.ProcessedBooksIds).Distinct();
            var score = 0;
            foreach (var b in booksIds)
            {
                score += bookScores[b];
            }

            Console.WriteLine(outPut + " - " + score + Environment.NewLine);
            File.WriteAllText(outPut, resultLibs.Count.ToString() + Environment.NewLine);
            foreach (var lib in resultLibs)
            {
                File.AppendAllText(outPut, $"{lib.Id} {lib.ProcessedBooksIds.Count}" + Environment.NewLine);
                File.AppendAllText(outPut, string.Join(" ", lib.ProcessedBooksIds) + Environment.NewLine);
            }

            MakeChart(workingLibs, outPut.Split('.')[0]);
            Process.Start(outPut.Split('.')[0] + ".png");
        }

        static void MakeChart(List<Library> libraries, string outputName)
        {
            var dataSet = new DataSet();
            DataTable dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Counter", typeof(int));

            for (int i = 0; i < libraries.Count; i++)
            {
                DataRow r2 = dt.NewRow();
                r2[0] = i + 1;
                r2[1] = libraries[i].Books.Where(b=>libraries[i].ProcessedBooksIds.Contains(b.Id)).Sum(b=>b.Score);
                dt.Rows.Add(r2);
            }

            dataSet.Tables.Add(dt);

            Chart chart = new Chart();
            chart.DataSource = dataSet.Tables[0];
            chart.Width = 2400;
            chart.Height = 1400;
            //create serie...
            Series serie1 = new Series();
            serie1.Name = "Serie1";
            serie1.Color = Color.FromArgb(112, 255, 200);
            serie1.BorderColor = Color.FromArgb(164, 164, 164);
            serie1.ChartType = SeriesChartType.Column;
            serie1.BorderDashStyle = ChartDashStyle.Solid;
            serie1.BorderWidth = 1;
            serie1.ShadowColor = Color.FromArgb(128, 128, 128);
            serie1.ShadowOffset = 1;
            serie1.IsValueShownAsLabel = true;
            serie1.XValueMember = "Name";
            serie1.YValueMembers = "Counter";
            serie1.Font = new Font("Tahoma", 8.0f);
            serie1.BackSecondaryColor = Color.FromArgb(0, 102, 153);
            serie1.LabelForeColor = Color.FromArgb(100, 100, 100);
            chart.Series.Add(serie1);
            //create chartareas...
            ChartArea ca = new ChartArea();
            ca.Name = "ChartArea1";
            ca.BackColor = Color.White;
            ca.BorderColor = Color.FromArgb(26, 59, 105);
            ca.BorderWidth = 0;
            ca.BorderDashStyle = ChartDashStyle.Solid;
            ca.AxisX = new Axis();
            ca.AxisY = new Axis();
            chart.ChartAreas.Add(ca);
            //databind...
            chart.DataBind();
            //save result...
            chart.SaveImage($"{outputName}.png", ChartImageFormat.Png);
        }

    }

    public class Library
    {
        public Library()
        {
            ProcessedBooksIds = new List<int>();
            BooksForBooking = new List<int>();
        }
        public int Id { get; set; }
        public int BooksCount { get; set; }
        public int SingupDays { get; set; }
        public int BooksPerDay { get; set; }
        public List<Book> Books { get; set; }
        public bool SignedUp { get; set; }
        public double Rate { get; set; }
        public double Rate2 { get; set; }
        public double Rate3 { get; set; }
        public double Rate4 { get; set; }
        public List<int> ProcessedBooksIds { get; set; }
        public bool Idle { get; set; }
        public IEnumerable<int> BooksForBooking { get; set; }
    }

    public class Book
    {
        public int Id { get; set; }
        public int Score { get; set; }
        public int UseCount { get; set; }
    }
}

