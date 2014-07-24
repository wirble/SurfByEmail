SurfByEmail
===========

Program by Trieu Nguyen July 2014.
Use at your own risk.  License under the MIT license.


Uses Phantomjs, htmlagilitypack and wkhtmltopdf third party to generate pdf/image.
Uses OpenPop for Pop mail connections.  Codes retrieved from code samples and modified.

A console windows app that monitors a given pop mail inbox for new message with url.  The program will fetch the page and convert to text, html, pdf or image, then email the file as an attachment to the sender.  This program allows a person without internet connection to surf the web.  For people with tight squeeze at work with internet blocked.  

The program will run constantly on your home computer and check your pop mail inbox.

How to use:

Send an email to the given email box.

Subject line has different commands:  [options]
	html (default) no subject line - all links in body will return as html
	help - will return a list of custom url already defined in .config file
	text - links will be returned as text only (still needs work)
	image - links will return as a .png file
	pdf - links will return a pdf 
	links - links will return a a pdf with links and table of content
	
	If multiple options are specify above, priority is given below.  Only one option is allow at a time.
		help>text>image>pdf>links
		
	Special url - these are predefined url with key words to use in the subject line.  Shortcuts if you will.  This way you just have to use the keywords instead of typing out the full url to lookup.  This is defined in the .config file.  Send a message with the subject "help" will also give you a listing of these shortcuts.
	
	Example:
		To return an html of a google search: Send an email with subject, "g:Selena+Gomez"
		To return an image of a google search: Send an email with subject, "g:Selena+Gomez image"
		To return a pdf of a stock quotes search: Send an email with subject, "mwquotes:mnkd,ino,bdsi"
		To return an html of google U.S. News section: Send an email with subject, "gus"
	
	Without using the shortcuts:

	To retrieve a url in html, send an empty subject line with the url in the body of the message:  

		http://cnn.com
		http://ebates.com
		So on...
                  
	Body of message should only contain one link on each line.
	
	To retrieve a url in image or pdf, specify that in the subject line instead of leaving it empty and include a url on each line in the body of the message.
	
	That's about it.
	
	
Settings

You can specify the accounts username and password as well as the frequency to check in the .config file.

You only need to click on the executable and the program will ask you to enter the account user name and password and it should be all you need to get started.  Other options are in the .config file.



Thanks
