using Octokit;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TMUpdate;
class Program
{
    // TMFFmpegApp.exe tuanmjnh/TMFFmpegApp_releases
    //static string zipPathTemp = Path.Combine(Path.GetTempPath(), "TMFFmpegApp.zip");
    static string currentVersion = null;
    static Release latestVersion = null;
    static void Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Exist");
                //return;
            }
            //isAppExe("owner/repoName");
            //Console.WriteLine("TM Update Application console running");
            //Console.WriteLine(string.Join("; ",args));

            // Get Version
            if (args.Length == 1 && (args[0] == "--version" || args[0] == "-v"))
            {
                var AppInformation = Assembly.GetExecutingAssembly().GetName();
                Console.WriteLine($"{AppInformation.Name} - {AppInformation.Version}");
            }
            else if (args.Length == 3 && ((args[0] == "--version" && args[1].Length > 0 && args[2].Length > 0) || (args[0] == "-v" && args[1].Length > 0 && args[2].Length > 0)))
            {
                //Current Version
                GetCurrentVersion(args[1]).Wait();
                Console.WriteLine($"Current {args[1]}: {currentVersion}");

                //Latest Version
                var repositories = args[2].Trim().Split('/');
                if (repositories.Length < 2)
                {
                    Console.WriteLine("repositories: owner/repoName, Please check input!");
                    return;
                }
                GetLatestVersion(repositories[0], repositories[1]).Wait();
                Console.WriteLine($"Latest {args[1]}: {latestVersion.TagName}");
            }
            else if (args.Length == 3 && ((args[0] == "--update" && args[1].Length > 0 && args[2].Length > 0) || (args[0] == "-u" && args[1].Length > 0 && args[2].Length > 0)))
            {
                /*
                 * args[0] = update
                 * args[1] = application.exe
                 * args[2] = repositories: owner/repoName
                */
                var repositories = args[2].Trim().Split('/');
                if (repositories.Length < 2)
                {
                    Console.WriteLine("repositories: owner/repoName, Please check input!");
                    return;
                }
                GetCurrentVersion(args[1]).Wait();
                GetLatestVersion(repositories[0], repositories[1]).Wait();
                if (int.Parse(currentVersion.Replace(".", "")) < int.Parse(latestVersion.TagName.Replace(".", "")))
                {
                    var downloadUrl = latestVersion.Assets[0].BrowserDownloadUrl;
                    var filenameSplit = downloadUrl.Split("/");
                    var fileOrigin = filenameSplit[filenameSplit.Length - 1];
                    var extFileOrigin = Path.GetExtension(fileOrigin);
                    var fileName = $"{fileOrigin.Replace(extFileOrigin, $"_{filenameSplit[filenameSplit.Length - 2]}")}{extFileOrigin}";
                    var zipPathTemp = Path.Combine(Path.GetTempPath(), fileName);
                    //
                    Console.WriteLine($"Downloading...");
                    DownloadRelease(fileName, latestVersion.Assets[0].BrowserDownloadUrl, zipPathTemp).Wait();
                    Console.WriteLine($"Downloaded!");
                    //
                    Console.WriteLine($"Updating...");
                    ExtractToDirectory(zipPathTemp, Environment.CurrentDirectory).Wait();
                    Console.WriteLine($"Updated!");
                    //
                    Console.WriteLine($"Deleting Temp file!");
                    DeleteTempPath(zipPathTemp).Wait();
                    Console.WriteLine($"Deleted Temp file!");
                    //
                    Console.WriteLine($"Update successful");
                    //
                    //Run application after update
                    Process.Start($"{Path.Combine(Environment.CurrentDirectory, args[1])}");
                }
                else
                {
                    Console.WriteLine($"Appliction up to date!");
                }
            }
            else if (args.Length == 2 && ((args[0] == "--version" && args[1].Length > 0) || (args[0] == "-v" && args[1].Length > 0)))
            {
                if (isAppExe(args[1]))
                {
                    GetCurrentVersion(args[1]).Wait();
                    Console.WriteLine($"Current {args[1]}: {currentVersion}");
                }
                else
                {
                    var repositories = args[1].Trim().Split('/');
                    if (repositories.Length < 2)
                    {
                        Console.WriteLine("repositories: owner/repoName, Please check input!");
                        return;
                    }
                    GetLatestVersion(repositories[0], repositories[1]).Wait();
                    Console.WriteLine($"Latest {args[1]}: {latestVersion.TagName}");
                }
            }
            //else if (args.Length == 3 && ((args[0] == "--version" && args[1] == "current" && args[2].Length > 0) || (args[0] == "-v" && args[1] == "c" && args[2].Length > 0)))
            //{
            //    GetCurrentVersion(args[2]);
            //    Console.WriteLine($"Current {args[2]}: {currentVersion}");
            //}
            //else if (args.Length == 3 && ((args[0] == "--version" && args[1] == "latest" && args[2].Length > 0) || (args[0] == "-v" && args[1] == "l" && args[2].Length > 0)))
            //{
            //    var repositories = args[2].Trim().Split('/');
            //    if (repositories.Length < 2)
            //    {
            //        Console.WriteLine("repositories: owner/repoName, Please check input!");
            //        return;
            //    }
            //    GetLatestVersion(repositories[0], repositories[1]).Wait();
            //    Console.WriteLine($"Latest {args[2]}: {latestVersion.TagName}");
            //}
            else if (args.Length == 3 && (args[0] == "--extract" || args[0] == "-e") && args[1].Length > 0 && args[2].Length > 0)
            {
                if (args[2] == "/" || args[2] == "\\") args[2] = Environment.CurrentDirectory;
                Console.WriteLine($"extracting zipPath: {args[1]} - destination: {args[2]}");
                ExtractToDirectory(args[1], args[2]).Wait();
                Console.WriteLine($"extracted zipPath: {args[1]} - destination: {args[2]}");
            }
            else if (args.Length == 4 && (args[0] == "--download" || args[0] == "-d") && args[1].Length > 0 && args[2].Length > 0 && args[3].Length > 0)
            {
                if (args[3] == "/" || args[3] == "\\") args[3] = Environment.CurrentDirectory;
                var destination = Path.Combine(args[3], args[1]);
                Console.WriteLine($"Downloading file: {args[1]} - from {args[2]} - destination: {destination}");
                DownloadRelease(args[1], args[2], destination).Wait();
                Console.WriteLine($"Downloaded file: {args[1]}- from {args[2]} - destination: {destination}");
            }
            else if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h"))
            {
                var rs = "TM Update help command\r\n";
                rs = $"{rs} [--version | -v] check version of TM Update \r\n";
                rs = $"{rs} [--version application.exe | -v application.exe] get current version for application\r\n";
                rs = $"{rs} [--version owner/repoName | -v owner/repoName] get latest version for application\r\n";
                rs = $"{rs} [--version | -v application.exe owner/repoName] get current version and latest version for application\r\n";
                rs = $"{rs} [--update | -u application.exe owner/repoName] update latest version for application";
                Console.WriteLine(rs);
            }
            else
            {
                Console.WriteLine($"TM Update for github Release.Help command: --help | -h");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message}\r\nTM Update for github Release.Help command: --help | -h");
        }
    }
    static async Task GetCurrentVersion(string exePath)
    {
        await Task.Run(() =>
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
            currentVersion = versionInfo.FileVersion;
        });
    }
    static async Task GetLatestVersion(string owner, string repoName)
    {
        var token = "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";
        //var owner = "tuanmjnh";
        //var repoName = "TMFFmpegApp_releases";
        var client = new GitHubClient(new Octokit.ProductHeaderValue(repoName));
        client.Credentials = new Credentials(token); // NOTE: not real token
        //var user = await client.User.Get(owner);
        //var tokenAuth = new Credentials(token);
        //client.Credentials = tokenAuth;
        //var releases = await client.Repository.Release.GetAll("tuanmjnh", "TMFFmpegApp_releases");
        //var latest = releases.ElementAt(0);
        //Console.WriteLine(
        //    "The latest release is tagged at {0} and is named {1}",
        //    latest.TagName,
        //    latest.Name);
        //var repository = await client.Repository.Get("octokit", "octokit.net");
        latestVersion = await client.Repository.Release.GetLatest(owner, repoName);
    }
    static async Task DownloadRelease(string fileName, string url, string zipPath)
    {
        //using (WebClient webClient = new WebClient())
        //{
        //    webClient.DownloadProgressChanged += DownloadProgressCallback;
        //    webClient.Credentials = CredentialCache.DefaultNetworkCredentials;
        //    await webClient.DownloadFileTaskAsync(new Uri(release.Assets[0].BrowserDownloadUrl), zipPath);
        //}
        WebClient webClient = new WebClient();
        //webClient.Headers.Add("user-agent", "Anything");
        //webClient.Headers.Add("authorization", "token " + GitHubToken);
        webClient.DownloadProgressChanged += (s, e) =>
        {
            //Console.WriteLine("{0} {1} - {2}. {3}% complete...", fileName, e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage);
            Console.WriteLine("{0} {1} - {2}. {3}% complete...",
                fileName,
                $"{ToSize(BytesPerSecond(e.BytesReceived), SizeUnits.KB)} {SizeUnits.KB}/s",
                $"{ToSize(e.BytesReceived, SizeUnits.MB)} of {ToSize(e.TotalBytesToReceive, SizeUnits.MB)} {SizeUnits.MB}",
                e.ProgressPercentage);
            //Thread.Sleep(10000);
        };
        webClient.Proxy = GlobalProxySelection.GetEmptyWebProxy();
        //webClient.Proxy = WebRequest.DefaultWebProxy;
        await webClient.DownloadFileTaskAsync(new Uri(url), zipPath);
    }
    static async Task DownloadRepository(string owner, string repoName)
    {
        var client = new GitHubClient(new Octokit.ProductHeaderValue(owner));
        var repository = await client.Repository.Get("octokit", "octokit.net");
    }
    static DateTime lastUpdate;
    static long BytesPerSecond(long bytes)
    {
        try
        {
            if (lastUpdate == default(DateTime))
            {
                lastUpdate = DateTime.Now;
                return 0;
            }
            else
            {
                var timeSpan = DateTime.Now - lastUpdate;
                if (timeSpan.TotalSeconds > 0)
                {
                    var bytesPerSecond = bytes / (long)timeSpan.TotalSeconds;
                    return bytesPerSecond;
                }
            }
            return 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }
    static async Task ExtractAppliction(string zipPath, string extractPath)
    {
        //string startPath = @"D:\applications\ffmpeg\TMFFmpegApp";
        //string zipPath = @"D:\applications\ffmpeg\TMFFmpegApp.zip";
        //string extractPath = @"D:\applications\ffmpeg\extract";

        //await Task.Run(() => System.IO.Compression.ZipFile.CreateFromDirectory(startPath, zipPath));
        //Console.WriteLine($"Created Zip");
        //await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath));
        //Console.WriteLine($"Extracted Zip");
        Console.WriteLine($"Unpacking...");
        await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath, true));
        Console.WriteLine($"Unpacked");
    }
    static async Task ExtractToDirectory(ZipArchive archive, string destinationDirectoryName, bool overwrite = true)
    {
        Console.WriteLine($"Unpacking...");
        if (!overwrite)
        {
            await Task.Run(() => archive.ExtractToDirectory(destinationDirectoryName));
            return;
        }

        DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
        string destinationDirectoryFullPath = di.FullName;
        await Task.Run(() =>
        {
            foreach (ZipArchiveEntry file in archive.Entries)
            {
                string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, file.FullName));

                if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException("Trying to extract file outside of destination directory. See this link for more info: https://snyk.io/research/zip-slip-vulnerability");
                }

                if (file.Name == "")
                {// Assuming Empty for Directory
                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                    continue;
                }
                file.ExtractToFile(completeFileName, true);
            }
        });
        Console.WriteLine($"Unpacked");
    }
    static async Task ExtractToDirectory(string zipPath, string destinationDirectoryName, bool overwrite = true)
    {
        await Task.Run(() =>
        {
            Console.WriteLine($"Unpacking...");
            //var fs = new FileStream(zipPath, System.IO.FileMode.Open);
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                if (!overwrite)
                {
                    archive.ExtractToDirectory(destinationDirectoryName);
                    return;
                }

                DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
                string destinationDirectoryFullPath = di.FullName;

                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    //
                    if (file.Name == "TMUpdate.exe") continue;
                    if (file.Name == "TMUpdate.pdb") continue; 
                    string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, file.FullName));

                    if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new IOException("Trying to extract file outside of destination directory. See this link for more info: https://snyk.io/research/zip-slip-vulnerability");
                    }

                    if (file.Name == "")
                    {// Assuming Empty for Directory
                        Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                        continue;
                    }
                    file.ExtractToFile(completeFileName, true);
                }
                Console.WriteLine($"Unpacked");
            }
        });
    }
    //static async Task ExtractToDirectory(string zipPath, string destinationDirectoryName, bool overwrite = true)
    //{
    //    await Task.Run(() =>
    //    {
    //        Console.WriteLine($"Unpacking...");
    //        using (var fs = new FileStream(zipPath, System.IO.FileMode.Create))
    //        using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
    //        {
    //            if (!overwrite)
    //            {
    //                archive.ExtractToDirectory(destinationDirectoryName);
    //                return;
    //            }

    //            DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
    //            string destinationDirectoryFullPath = di.FullName;

    //            foreach (ZipArchiveEntry file in archive.Entries)
    //            {
    //                string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, file.FullName));

    //                if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
    //                {
    //                    throw new IOException("Trying to extract file outside of destination directory. See this link for more info: https://snyk.io/research/zip-slip-vulnerability");
    //                }

    //                if (file.Name == "")
    //                {// Assuming Empty for Directory
    //                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
    //                    continue;
    //                }
    //                file.ExtractToFile(completeFileName, true);
    //            }
    //            Console.WriteLine($"Unpacked");
    //        }
    //    });
    //}
    static async Task<bool> DeleteTempPath(string zipPath)
    {
        if (File.Exists(zipPath))
        {
            await Task.Run(() => File.Delete(zipPath));
            return true;
        }
        return true;
    }
    static bool isAppExe(string s)
    {
        string pattern = @"^(.*).exe$";
        //string pattern = @"(youtu.*be.*)\/(watch\?v=|embed\/|shorts|)(.*?((?=[&#?])|$))|(youtu.*be.*)\/(@.*|)\/";
        var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var rs = regex.Match(s);
        if (rs.Success) return true;//.Groups[2].Value +" "+ rs.Groups[6].Value;
        else return false;
    }
    public enum SizeUnits
    {
        Byte, KB, MB, GB, TB, PB, EB, ZB, YB
    }

    public static string ToSize(Int64 value, SizeUnits unit)
    {
        return (value / (double)Math.Pow(1024, (Int64)unit)).ToString("0.00");
    }
}