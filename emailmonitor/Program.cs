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

using System.Web;
using System.Threading;

using System.Text;
using System.Net.Mail;
using System.Net;

using OpenPop.Mime;
using OpenPop.Mime.Traverse;
using Message = OpenPop.Mime.Message;
using System.Collections.Generic;
using OpenPop.Pop3;
using OpenPop.Common.Logging;
using System.IO;
using OpenPop.Mime.Header;
using System.Text.RegularExpressions;
using System.Resources;
using System.Collections;



namespace SurfByEmail
{
    class Program
    {
        static void Main(string[] args)
        {
            //checkConfigruation();
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
            checkSpecialURL(subjectCmd);

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

                    subjectCmds = parseSubjectLine(subjectemail);

                    // Only want to download message if:
                    //  - is from whitelist
                    //  - has subject "web"
                    if (from.HasValidMailAddress && IsInWhiteList(msgs[i].Headers.From.Address))
                    {
                        List<String> urls = FindURLSInMessage(msgs[i]);


                        savedFile = SiteToPDF(urls, RemoveSpecialCharacters(from.MailAddress.Address.ToString()), subjectCmds);

                        if (savedFile.Count > 0)
                        {
                            Console.WriteLine("Sending emails");
                            SendMail(savedFile, from.MailAddress.Address);
                        }
                        else
                        {
                            Console.WriteLine("no file to email" + savedFile.Count.ToString());
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

            while (enumerator.MoveNext())
            {
                spUrl.Add(((System.Configuration.SettingsProperty)enumerator.Current).Name.ToLower().ToString(), ((System.Configuration.SettingsProperty)enumerator.Current).DefaultValue.ToString().ToLower());
            }

            return url;
        }
        private static void checkConfigruation(){

            if(Properties.Settings.Default.PopUserEmail=="" ||Properties.Settings.Default.PopPassword == "" ||
                Properties.Settings.Default.SmtpUserEmail == "" || Properties.Settings.Default.SmtpPassword=="")
            {
                Console.WriteLine("You need to update the email account username and password.  If you don't want to add this on startup, edit the .config file.");
                Console.WriteLine("Please enter email address of the monitoring account: ");
                Properties.Settings.Default.PopUserEmail = Console.ReadLine();
                Console.WriteLine("Please enter password of the monitoring account: ");
                Properties.Settings.Default.PopPassword = Console.ReadLine();
                Console.WriteLine("Do you want to use the same account to send the pdf/image? (y/n)");
                string sameAccount = Console.ReadLine();

                if (sameAccount.ToLower() == "yes" || sameAccount.ToLower()=="y")
                {

                    Properties.Settings.Default.SmtpUserEmail = Properties.Settings.Default.PopUserEmail;
                    Properties.Settings.Default.SmtpPassword = Properties.Settings.Default.PopPassword;
                }
                else
                {
                    Console.WriteLine("Please enter email address of the sending account: ");
                    Properties.Settings.Default.SmtpUserEmail = Console.ReadLine();
                    Console.WriteLine("Please enter email password of the sending account: ");
                    Properties.Settings.Default.SmtpPassword = Console.ReadLine();
                }
            }


        }
        private static Dictionary<String,String> parseSubjectLine(String subjectemail)
        {
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
                    }
                }
            }
            return subjectCmds;
        }
        public static void SendMail(List<String> pdfFile,String sendAddress)
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
        public static string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
        }
        internal static bool FileOrDirectoryExists(string name)
        {
            return (Directory.Exists(name) || File.Exists(name));
        }
       
        private static List<String> SiteToPDF(List<String> url, String fromFolder, Dictionary<String, String> fileType)
        {
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

            String extension = ".png";
            if (fileType.ContainsKey("type"))
            {
                if (fileType["type"] == "pdf" )
                {
                    extension = ".pdf";
                }

            }
            else if (fileType.ContainsKey("links"))
            {
                if (fileType["links"] == "yes")
                {
                    extension = ".pdf";
                }
            }

            string filename = "";
            var filePath = "";
            List<String> fileList = new List<String>();

            for (int k = 0; k < url.Count; k++)
            {
                //string serverPath = fromFolder;
                filename = RemoveSpecialCharacters(url[k]) + extension; //DateTime.Now.ToString("ddMMyyyy_hhmmss.fff") + ".pdf";
                if (filename.Length > 25)//limit file name to just 25 characters
                    filename = RemoveSpecialCharacters(url[k]).Substring(0, 25) + extension;

                filePath = Path.Combine(fromFolder, filename);

                bool executeDefault = true;

                if (fileType.ContainsKey("links"))
                {
                    if (fileType["links"] == "yes")
                    {
                        executeDefault = false;

                    }

                }
                if (executeDefault)//without links
                {
                    ExecuteCommand("cd " + fromFolder + " & ..\\phantomjs ..\\rasterize.js " + url[k] + " " + filename + " \"A4\"");
                }
                else //with links
                {
                    ExecuteCommand("cd " + fromFolder + " & ..\\wkhtmltopdf.exe " + url[k] + " " + filename);

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

        /// <summary>
        /// Example showing:
        ///  - how to use UID's (unique ID's) of messages from the POP3 server
        ///  - how to download messages not seen before
        ///    (notice that the POP3 protocol cannot see if a message has been read on the server
        ///     before. Therefore the client need to maintain this state for itself)
        /// </summary>
        /// <param name="hostname">Hostname of the server. For example: pop3.live.com</param>
        /// <param name="port">Host port to connect to. Normally: 110 for plain POP3, 995 for SSL POP3</param>
        /// <param name="useSsl">Whether or not to use SSL to connect to server</param>
        /// <param name="username">Username of the user on the server</param>
        /// <param name="password">Password of the user on the server</param>
        /// <param name="seenUids">
        /// List of UID's of all messages seen before.
        /// New message UID's will be added to the list.
        /// Consider using a HashSet if you are using >= 3.5 .NET
        /// </param>
        /// <returns>A List of new Messages on the server</returns>



    }
}
