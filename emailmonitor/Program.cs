/*email monitor is surf by web windows console program.  It is designed to be ran by user and monitor 
 * their gmail inbox for incoming mail with url link.  The program will process those links, query the web
 * and generate a pdf, pdf with links or an images of the site.
 * 
 * This program is mainly designed to allow people with no internet at work and with only access to mail;
 * allowing them to surf the web.
 * 
 * This program uses:
 * 1. OpenPop.Net - to retrieves incoming messages. Codes to retrieved were cut/paste and modified.
 * http://hpop.sourceforge.net/examples.php
 * 2. Phantomjs - to generate pdf or images via console execution
 * http://phantomjs.org/
 * 3. wkhtmltopdf - to generate pdf with links
 * http://wkhtmltopdf.org/
 * 
 * no warranty is expressed or implied, use at your own risk
 * Coded by Trieu Nguyen
 * email:wirble at gmail dot com
 * Date: 7/19/2014
 * 
 * 
 * /*/


using System;
using System.Diagnostics;
using System.Configuration;
using System.Threading;

using System.Net.Mail;
using System.Net;

using OpenPop.Mime;
using Message = OpenPop.Mime.Message;
using System.Collections.Generic;
using OpenPop.Pop3;
using System.IO;
using OpenPop.Mime.Header;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Runtime.Serialization.Formatters.Binary;




namespace SurfByEmail
{
    class Program
    {
        static void Main(string[] args)
        {
            checkConfigruation();
            writeListofSpecialUrl();
            Program p = new Program();
            Thread thread = new Thread(p.Run);
            thread.Start();


            Console.Out.WriteLine("Press any key to stop the execution ...");
            Console.ReadKey(); //main thread blocks here
            thread.Abort();

            
            

            //Console.Write(fileNames);
        }
        private void Run()
        {
            for (; ; )
            {
                try
                {
                    
                    checkMessage();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                }
                Thread.Sleep(Properties.Settings.Default.WaitTimeToRecheckPop);
            }

        }
        private static void checkMessage()
        {

            List<Message> msgs = new List<Message>();
            
            Dictionary<String, String> subjectCmd = new Dictionary<string, string>();
            //subjectCmd.Add("g", "whatisthistest");
            //subjectCmd.Add("links", "yes");
            //subjectCmd.Add("gTech", "");
            //subjectCmd.Add("twitter", "trueguy");

            //checkSpecialURL(subjectCmd);

            msgs = FetchAndDeleteAllMessages(Properties.Settings.Default.PopServer, Properties.Settings.Default.PopPort, Properties.Settings.Default.PopSSL, Properties.Settings.Default.PopUserEmail, Properties.Settings.Default.PopPassword);

            //String fileNames = SiteToPDF("http://google.com", "test");
            if (msgs.Count > 0)
            {


                for (int i = 0; i < msgs.Count; i++)
                {

                    RfcMailAddress from = msgs[i].Headers.From;
                    string subjectemail = msgs[i].Headers.Subject;
                    List<String> savedFile = new List<String>();
                    string msgID = msgs[i].Headers.MessageId;

                    Dictionary<String, String> subjectCmds = new Dictionary<string, string>();



                    Console.WriteLine("Message from: " + from.Address.ToString() + " Subject: " + subjectemail);
                    // Only want to download message if:
                    //  - is from whitelist
                    //  - has subject "web"
                    if (from.HasValidMailAddress && IsInWhiteList(msgs[i].Headers.From.Address))
                    {
                        subjectCmds = parseSubjectLine(subjectemail);

                        List<String> urls = new List<String>();
                        List<String> spurls = checkSpecialURL(subjectCmds);

                        if (spurls.Count == 0)
                            urls = FindURLSInMessage(msgs[i]);
                        else
                            urls = spurls;

                        savedFile = SiteToPDF(urls, RemoveSpecialCharacters(from.MailAddress.Address.ToString()), subjectCmds);

                        if (savedFile.Count > 0)
                        {
                            Console.WriteLine("Sending emails");
                            SendMail(savedFile, from.MailAddress.Address);
                        }
                        else
                        {
                            Console.WriteLine("No file to email.  " + savedFile.Count.ToString() + " file was created.");
                        }

                    }


                }
            }
            else
            {
                Console.WriteLine("No new message");
            }
        }
        private static List<String> checkSpecialURL(Dictionary<String,String> subject){

            System.Collections.IEnumerator enumerator = SpecialUrl.Default.Properties.GetEnumerator();
            
            Dictionary<String, String> spUrl = new Dictionary<String, String>();
            List<String> url = new List<String>();
            //lets get the configuration file into memory
            while (enumerator.MoveNext())
            {
                spUrl.Add(((System.Configuration.SettingsProperty)enumerator.Current).Name.ToLower().ToString(), ((System.Configuration.SettingsProperty)enumerator.Current).DefaultValue.ToString().ToLower());
            }

            //match up the subject line to teh configuration file if any
            //subject cmds or configuration file shouldn't have any name conflict with the app config or will run into issue

            foreach(KeyValuePair<String,String> cmd in subject)
            {//loop through subject line command from email

                if (spUrl.ContainsKey(cmd.Key.ToString().ToLower()))
                {//if subject cmd matches the config

                    String tempUrl = spUrl[cmd.Key.ToString().ToLower()];//get the url from config
                    url.Add(tempUrl.Replace("replaceme", cmd.Value.ToString().ToLower()));//replace url with value from subject if any
                    //url.Add(tempUrl);//add to url list
                }


            }


                return url;
        }
        private static void checkConfigruation(){

            //String configPath = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming).FilePath;
            //Console.WriteLine("Configuration file location: ");
            //Console.WriteLine(configPath);
            //Console.WriteLine("");

            if (!File.Exists("phantomjs.exe"))
            {
                Console.WriteLine("You need to have the phantomjs.exe file in the same directory as the executable.");
                Thread.Sleep(7000);
                Environment.Exit(0);
            }
            if (!File.Exists("wkhtmltopdf.exe"))
            {
                Console.WriteLine("You need to have the wkhtmltopdf.exe and .dll file in the same directory as the executable.");
                Thread.Sleep(7000);
                Environment.Exit(0);
            }
            if(Properties.Settings.Default.PopUserEmail=="" ||Properties.Settings.Default.PopPassword == "" ||
                Properties.Settings.Default.SmtpUserEmail == "" || Properties.Settings.Default.SmtpPassword=="")
            {
                Console.WriteLine("You need to update the email account username and password.  If you don't want to add this on startup, edit the .config file.");
                Console.WriteLine("");
                Console.WriteLine("Please enter email address or username of the monitoring account: ");
                Properties.Settings.Default.PopUserEmail = Console.ReadLine();
                Console.WriteLine("Please enter password of the monitoring account: ");
                Properties.Settings.Default.PopPassword = Console.ReadLine();
                Console.WriteLine("");
                Console.WriteLine("Do you want to use the same account to send the pdf/image? (y/n)");
                string sameAccount = Console.ReadLine();

                if (sameAccount.ToLower() == "yes" || sameAccount.ToLower()=="y")
                {

                    Properties.Settings.Default.SmtpUserEmail = Properties.Settings.Default.PopUserEmail;
                    Properties.Settings.Default.SmtpPassword = Properties.Settings.Default.PopPassword;
                }
                else
                {
                    Console.WriteLine("");
                    Console.WriteLine("Please enter email address or username of the sending account: ");
                    Properties.Settings.Default.SmtpUserEmail = Console.ReadLine();
                    Console.WriteLine("Please enter email password of the sending account: ");
                    Properties.Settings.Default.SmtpPassword = Console.ReadLine();
                }
                Console.Clear();
            }


        }
        private static Dictionary<String,String> parseSubjectLine(String subjectemail)
        {//returns all lower cases
            Dictionary<String, String> subjectCmds = new Dictionary<string, string>();
            

            if (subjectemail != "" && subjectemail!=null)
            {
                
                String[] subjectEmailArray = subjectemail.Split(' ');
                if (subjectEmailArray.Length > 0)
                {

                    for (int k = 0; k < subjectEmailArray.Length; k++)
                    {
                        String[] cmd = subjectEmailArray[k].Split(':');
                        if (cmd.Length > 1)
                        {
                            subjectCmds.Add(cmd[0].ToLower(), cmd[1].ToLower());
                        }
                        else if (cmd.Length == 1)
                        {
                            subjectCmds.Add(cmd[0].ToLower(), "");
                        }
                    }
                }
            }
            return subjectCmds;
        }
        private static void SendMail(List<String> pdfFile,String sendAddress)
        {
            MailAddress fromAddress = new MailAddress(Properties.Settings.Default.SmtpUserEmail, Properties.Settings.Default.SmtpUserEmail);
            MailAddress toAddress = new MailAddress(sendAddress, sendAddress);
            string fromPassword = Properties.Settings.Default.SmtpPassword;
            string subject = "html pages";
            string body = "Here are your pages.";
            System.Net.Mail.Attachment attachment;
           


            var smtp = new SmtpClient
            {
                Host = Properties.Settings.Default.SmtpServer,
                Port = Properties.Settings.Default.SmtpPort,
                EnableSsl = Properties.Settings.Default.SmtpSSL,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {


                Subject = subject,
                Body = body
            })
            {

                for (int i = 0; i < pdfFile.Count; i++)
                {
                    bool addSuccess = false;
                    while (!addSuccess)
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(1000);
                            attachment = new System.Net.Mail.Attachment(pdfFile[i]);
                            message.Attachments.Add(attachment);
                            addSuccess = true;
                        }
                        catch (System.IO.IOException ex)
                        {
                            Console.WriteLine("Attaching file:  File not processed yet. Wait...");
                           

                        }
                    }
                }
                try
                {

                    smtp.Send(message);
                }
                catch (System.Net.Mail.SmtpException ex)
                {
                    Console.WriteLine("Error sending mail: " + ex.Message.ToString());

                }
            }


        }
        private static string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
        }
        internal static bool FileOrDirectoryExists(string name)
        {
            return (Directory.Exists(name) || File.Exists(name));
        }
        private static void writeListofSpecialUrl()
        {

            System.Collections.IEnumerator enumerator = SpecialUrl.Default.Properties.GetEnumerator();

            Dictionary<String, String> spUrl = new Dictionary<String, String>();
            List<String> url = new List<String>();
           
            //lets get the configuration file into memory

            // Save data
            //System.IO.File.WriteAllText(@"help.txt", urlListing);
            using (StreamWriter writer = new StreamWriter(@"help.txt", append:false))
            {
                writer.WriteLine("Other subject line commands");
                writer.WriteLine("");
                writer.WriteLine("If you don't specify a return file type.  It will default to .html file for the url.");
                writer.WriteLine("help - if this is specify this will take first priority.");
                writer.WriteLine("text - if this is specify, this will take priority second to help and so on and so forth.");
                writer.WriteLine("help>text>image>pdf>links are all valid subject line commands for return types.");
                writer.WriteLine("image - will generate a png version of the pdf.");
                writer.WriteLine("pdf - will generate non link version of the pdf.");
                writer.WriteLine("links - will generate a link version of the pdf.");
                writer.WriteLine("");
                writer.WriteLine("");
                writer.WriteLine("The simplest email is to send a blank subject with a url in the body and the program will generate an html file from the monitored account to the sender's email address.");
                writer.WriteLine("Sending an email with the subject, help, will retrieve a text file containing these messages.");
                writer.WriteLine("");
                writer.WriteLine("");

                writer.WriteLine("List of pre-config. key word search.");
                writer.WriteLine("Url with 'replaceme', you can add text for the search parameter by using the keyword:texttosearch.");
                writer.WriteLine("For Example: You can do a google search by having this in the subject line, g:mygooglesearch, this will return the search result in html.");
                writer.WriteLine("If you want to do a stock quote search, quote:MNKD image, this will return a quote page in an image format.");
                writer.WriteLine("This will override any url in the body message.  You just need to have these keywords in the subject line.");
                writer.WriteLine("");
                writer.WriteLine("");
                writer.WriteLine("Subject Keyword       Url_returns");
                writer.WriteLine("");
                while (enumerator.MoveNext())
                {

                    writer.WriteLine(((System.Configuration.SettingsProperty)enumerator.Current).Name.ToLower().ToString() + " " + ((System.Configuration.SettingsProperty)enumerator.Current).DefaultValue.ToString().ToLower());
                }
               
            }
            
        }
        private static List<String> SiteToPDF(List<String> url, String fromFolder, Dictionary<String, String> fileType)
        {
            string filename = "";
            string filePath = "";
            List<String> fileList = new List<String>();


            //create the directly based on the from address
            if (!Directory.Exists(fromFolder))
            {
                Directory.CreateDirectory(fromFolder);
            }
            else
            {
                //delete existing files
                Array.ForEach(Directory.GetFiles(@fromFolder), File.Delete);
            }
            //default to html pages
            String extension = ".html";

            if (fileType.ContainsKey("help"))
            {//help command in subject will override all others
                extension = "help";
                Console.WriteLine("Help requested only.");
                filePath = "help.txt";
                fileList.Add(filePath);
                return fileList;
            }
            else if (fileType.ContainsKey("text"))
            {//priority commands
                extension = ".txt";

            }
            else if (fileType.ContainsKey("image"))
            {//specifying pdf will make it pdf otherwise default to png

                extension = ".png";

            }
            else if (fileType.ContainsKey("pdf"))
            {//specifying pdf will make it pdf otherwise default to png

                    extension = ".pdf";

            }
            else if (fileType.ContainsKey("links"))
            {
                 extension = ".pdf";

            }


            for (int k = 0; k < url.Count; k++)
            {

                
                //string serverPath = fromFolder;
                filename = RemoveSpecialCharacters(url[k]) + extension; //DateTime.Now.ToString("ddMMyyyy_hhmmss.fff") + ".pdf";
                if (filename.Length > 25)//limit file name to just 25 characters
                    filename = RemoveSpecialCharacters(url[k]).Substring(0, 25) + extension;

                filePath = Path.Combine(fromFolder, filename);

                //html only
                if (extension == ".html")
                {
                    using (WebClient client = new WebClient()) // WebClient class inherits IDisposable
                    {
                        Console.WriteLine("Grabbing html: " + url[k].Replace("\"",""));
                        client.DownloadFile(url[k].Replace("\"", ""), @filePath);

                    }
                 

                }
                else if (extension == ".txt")
                {//text
                    using (WebClient client = new WebClient()) // WebClient class inherits IDisposable
                    {

                        Console.WriteLine("Grabbing text only: " + url[k]);
                        HtmlWeb web = new HtmlWeb();
                        HtmlDocument doc = web.Load(url[k]);
                        string page = "";

                        foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//text()[normalize-space(.) != '']"))
                        {
                            page+=node.InnerText.Trim();
                        }

                        File.WriteAllText(@filePath, page);
                    }


                }else{//pdf and images
                    bool executeDefault = true;
                    Console.WriteLine("Grabbing pdf or images: " + url[k]);
                    if (fileType.ContainsKey("links"))
                    {
                        executeDefault = false;

                    }
                    if (executeDefault)//without links
                    {
                        ExecuteCommand("cd " + fromFolder + " & ..\\phantomjs ..\\rasterize.js " + url[k] + " " + filename + " \"A4\"");
                    }
                    else //with links
                    {
                        ExecuteCommand("cd " + fromFolder + " & ..\\wkhtmltopdf.exe -l -n " + url[k] + " " + filename);

                    }

                    int i = 0;
                    while (!FileOrDirectoryExists(filePath))
                    {

                        Thread.Sleep(800);
                        Console.WriteLine("Still fetching files. " + filePath);
                        i++;
                        if (i > 40)//file doesn't get made fast enough so lets wait until it's done 
                        {
                            //no file created
                            filePath = "";

                            break;
                        }
                        //do nothing return "";
                    }
                }

                if(filePath!="" && filePath != null)
                    fileList.Add(filePath);
                
            }
            return fileList;
        }
        private static void ExecuteCommand(string Command)
        {
            try
            {
                ProcessStartInfo ProcessInfo;
                Process Process;

                ProcessInfo = new ProcessStartInfo("cmd.exe", "/K " + Command);
                ProcessInfo.CreateNoWindow = true;
                ProcessInfo.UseShellExecute = false;

                Process = Process.Start(ProcessInfo);
            }
            catch { }
        }


        private static byte[] DoWhile(string filePath)
        {
            byte[] bytes = new byte[0];
            bool fail = true;

            while (fail)
            {
                try
                {
                    using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        bytes = new byte[file.Length];
                        file.Read(bytes, 0, (int)file.Length);
                    }

                    fail = false;
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }

            System.IO.File.Delete(filePath);
            return bytes;
        }


        public static bool IsInWhiteList(string from)
        {
            //bool isWhiteList = false;
            if (Properties.Settings.Default.FromWhiteList != "")
            {
                string[] whiteList = Properties.Settings.Default.FromWhiteList.Split(',');

                if (whiteList.Length > 0)
                {
                    for (int i = 0; i < whiteList.Length; i++)
                    {
                        if (from.ToLower() == whiteList[i].ToLower())
                        {
                            return true;
                        }


                    }
                    //no match
                    return false;
                }
                
            }



            //when blank
            return true;


        }
        /// <summary>
        /// Example showing:
        ///  - how to a find plain text version in a Message
        ///  - how to save MessageParts to file
        /// </summary>
        /// <param name="message">The message to examine for plain text</param>
        public static List<String> FindURLSInMessage(Message message)
        {
            List<String> urls = new List<String>();

            String line = "";
            
            MessagePart plainText = message.FindFirstPlainTextVersion();
            
            if (plainText != null)
            {
                var stream = new StreamReader(new MemoryStream(plainText.Body));
                while((line = stream.ReadLine()) != null)
                {
                    if (line.ToLower().Contains("http://") || line.ToLower().Contains("https://") || line.ToLower().Contains("file://"))
                    {
                        urls.Add(line.ToLower());
                    }
                }
                //stream.ReadToEnd();
                // Save the plain text to a file, database or anything you like
                //plainText.Save(new FileInfo("plainText.txt"));
                
            }
            return urls;
        }
        /// <summary>
        /// Example showing:
        ///  - how to fetch all messages from a POP3 server
        /// </summary>
        /// <param name="hostname">Hostname of the server. For example: pop3.live.com</param>
        /// <param name="port">Host port to connect to. Normally: 110 for plain POP3, 995 for SSL POP3</param>
        /// <param name="useSsl">Whether or not to use SSL to connect to server</param>
        /// <param name="username">Username of the user on the server</param>
        /// <param name="password">Password of the user on the server</param>
        /// <returns>All Messages on the POP3 server</returns>
        public static List<Message> FetchAndDeleteAllMessages(string hostname, int port, bool useSsl, string username, string password)
        {
            // The client disconnects from the server when being disposed
            using (Pop3Client client = new Pop3Client())
            {
                // Connect to the server
                client.Connect(hostname, port, useSsl);
                //OpenPop.Pop3.Exceptions.InvalidLoginException
                // Authenticate ourselves towards the server
                try
                {
                    client.Authenticate(username, password);
                }
                catch (Exception ex)
                {
                    throw;
                    //do nothing
                }
                // Get the number of messages in the inbox
                int messageCount = client.GetMessageCount();

                // We want to download all messages
                List<Message> allMessages = new List<Message>(messageCount);

                // Messages are numbered in the interval: [1, messageCount]
                // Ergo: message numbers are 1-based.
                // Most servers give the latest message the highest number
                for (int i = messageCount; i > 0; i--)
                {
                    allMessages.Add(client.GetMessage(i));
                    
                }
                //client.DeleteAllMessages();  this is not working
                    // Now return the fetched messages
                    return allMessages;
            }
        }




    }
}
