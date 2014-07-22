SurfByEmail
===========

Program by Trieu Nguyen July 2014.
Use at your own risk.  License under the MIT license.


Uses Phantomjs and wkhtmltopdf third party to generate pdf/image.
Uses OpenPop for Pop mail connections.  Codes retrieved from code samples and modified.

A console windows app that monitors a given gmail inbox for new message with url.  The program will fetch the page and convert to pdf or image, then email the file as an attachment to the sender.  This program allows a person without internet connection to surf the web.  For people with tight squeeze at work with internet blocked.  

The program will run constantly on your home computer and check your gmail inbox.

How to use:

Send an email to the given email box.

Subject line has two commands:  links:yes, type:pdf

Body of message:  

http://cnn.com
                  
http://ebates.com
                  
So on...
                  
If message has blank subject, return will be an image of the site
links:yes will give you a pdf with clickable links regardless of type
type:pdf will allow you to have a pdf but no clickable links, the links:yes will override this option

Body of message should only contain one link on each line.

You can specify the accounts username and password as well as the frequency to check in the .config file.

You only need to click on the executable and the program will ask you to enter the account user name and password and it should be all you need to get started.  Other options are in the .config file.

Other features to add:
1. text only returns
2. default searches such as google news in different area (top stories, etc...)
3. weather
4. stock prices
5. ...

Thanks
