/*
 * Copyright (C) 2011 SharpServ <https://github.com/painejake/SharpServ/>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 */

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SharpServ
{
	class WebServer
	{		
		private TcpListener sListener;

		// General Configuration ///////////////////////////////////
		// Maybe better to have String xxx not private string
		// Bind server to port 80 and 127.0.0.1
		Int32 port = 80;
		IPAddress localAddr = IPAddress.Parse("127.0.0.1");
		// Directory and config file locationsc
		private string cDefaultConfig = "C:\\www\\data\\default.txt";
		private string cVirtualConfig = "C:\\www\\data\\vdir.txt";
		private string cMIMETypConfig = "C:\\www\\data\\mime.txt";
		private string sWebServerRoot = "C:\\www\\";
		////////////////////////////////////////////////////////////
		
		// Build version to string
		private string pVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		
		// Displays CPU arch e.g x86
		private string cpuArch = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE", EnvironmentVariableTarget.Machine);
		
		// As it says on the tin
		OperatingSystem os = Environment.OSVersion;
		
		public WebServer()
		{ 
			try
			{
				// Start listening on selected port
				sListener = new TcpListener(localAddr, port);
				sListener.Start();
				
                //Version sVersion = new Version(Application.ProductVersion);
				//Console.WriteLine("SharpServ" + "_v" + sVersion.Major + "." + sVersion.Minor + "_OS_" + cpuArch + "_r" + sVersion.Revision);
				
				Console.WriteLine("SharpServ/" + pVersion + " " + os.Platform + " " + cpuArch + "\n");
				Console.WriteLine("Bind address: " + localAddr + ":" + port + "\n");
				Console.WriteLine("Press Ctrl + C to stop the server...\n");
				
				// Start the thread which casll the methods 'StartListen'
				Thread lThread = new Thread(new ThreadStart(StartListen));
				lThread.Start();
			}
			catch(Exception e)
			{
				Console.WriteLine("An exception occured while listening: " + e.ToString());
			}
		}
		
		/// <summary>
		/// Returns the default file name
		/// Input is the WebServerRoot folder
		/// Output is the default file name
		/// </summary>
		/// <param name="sLocalDirectory">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public string GetDefaultFileName(string sLocalDirectory)
		{
			StreamReader sReader;
			String sLine = "";
			
			try
			{
				// Here we open Default.txt to find out
				// the default files to serve
				sReader = new StreamReader(cDefaultConfig);
				
				while ((sLine = sReader.ReadLine()) !=null)
				{
					// Look for default file in the web server
					// root folder
					if (File.Exists(sLocalDirectory + sLine) == true)
						break;
				}
			}
			catch(Exception e)
			{
				Console.WriteLine("An exception occured: " + e.ToString());
			}
			if(File.Exists(sLocalDirectory + sLine) == true)
				return sLine;
			else
				return "";
		}
		
		/// <summary>
		/// This function takes FileName as input and returns the MIME type. 
		/// </summary>
		/// <param name="sRequestedFile">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public string GetMimeType(string sRequestedFile)
		{
			StreamReader sReader;
			String sLine = "";
			String sMimeType = "";
			String sFileExt = "";
			String sMimeExt = "";
			
			// Convert to lowercase
			sRequestedFile = sRequestedFile.ToLower();
			int iStartPos = sRequestedFile.IndexOf(".");
			sFileExt = sRequestedFile.Substring(iStartPos);
			
			try
			{
				// Open the Vdir.txt to find out the list
				// virtual directories
				sReader = new StreamReader(cMIMETypConfig);
				
				while((sLine = sReader.ReadLine()) !=null)
				{
					sLine.Trim();
					
					if(sLine.Length > 0)
					{
						// Find the seperator
						iStartPos = sLine.IndexOf(";");
						
						// Convert to lower case
						sLine = sLine.ToLower();
						
						sMimeExt = sLine.Substring(0,iStartPos);
						sMimeType = sLine.Substring(iStartPos + 1);
						
						if(sMimeExt == sFileExt)
							break;
					}
				}
			}
			catch(Exception e)
			{
				Console.WriteLine("An exception occured: " + e.ToString());
			}
			
			if(sMimeExt == sFileExt)
				return sMimeType;
			else
				return "";
		}
		
		/// <summary>
		/// Returns the physical path
		/// </summary>
		/// <param name="webServerRoot">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="sDirName">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public string GetLocalPath(string webServerRoot, string sDirName)
		{
			StreamReader sReader;
			String sLine = "";
			String sVirtualDir = "";
			String sActualDir = "";
			int iStartPos = 0;
			
			// Remove any extra spaces
			sDirName.Trim();
			
			// Convert to lowercase
			webServerRoot = webServerRoot.ToLower();
			sDirName = sDirName.ToLower();
			
			// Remove the slash
			sDirName = sDirName.Substring(1,sDirName.Length - 2);
			
			try
			{
				// Here we open the Vdir.txt to find out
				// the list of virtual directories
				sReader = new StreamReader(cVirtualConfig);
				
				while((sLine = sReader.ReadLine()) !=null)
				{
					// Remove any extra spaces
					sLine.Trim();
					
					if(sLine.Length > 0)
					{
						// Find the separator
						iStartPos = sLine.IndexOf(";");
						
						// Convert to lowercase
						sLine = sLine.ToLower();
						
						sVirtualDir = sLine.Substring(0,iStartPos);
						sActualDir = sLine.Substring(iStartPos + 1);
						
						if(sVirtualDir == sDirName)
						{
							break;
						}
					}
				}
			}
			catch(Exception e)
			{
				Console.WriteLine("An exception occured: " + e.ToString());
			}
			
			Console.WriteLine("Virtual: " + sVirtualDir);
			Console.WriteLine("Directory: " + sDirName);
			Console.WriteLine("Physical: " + sActualDir);
			if(sVirtualDir == sDirName)
				return sActualDir;
			else
				return "";
		}
		
		/// <summary>
		/// This function sends the header information to the client (browser)
		/// </summary>
		/// <param name="sHTTPVersion">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="sMIMEHeader">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="iTotBytes">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="sStatusCode">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="sSocket">
		/// A <see cref="Socket"/>
		/// </param>
		public void SendHeader(string sHTTPVersion, string sMIMEHeader,
		                       int iTotBytes, string sStatusCode, ref Socket sSocket)
		{
			String sBuffer = "";
			
			// If MIME type is not provided set default to text/html
			if(sMIMEHeader.Length == 0)
			{
				sMIMEHeader = "text/html";
			}
			
			// Server information - In future will pull this from a
			// config file when package is built
			sBuffer = sBuffer + sHTTPVersion + sStatusCode + "\r\n";
			sBuffer = sBuffer + "Server: SharpServ/" + pVersion + "\r\n";
			sBuffer = sBuffer + "Content-Type: " + sMIMEHeader + "\r\n";
			sBuffer = sBuffer + "Accept-Ranges: bytes\r\n";
			sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n\r\n";
			
			Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
			
			SendToBrowser(bSendData, ref sSocket);
			
			Console.WriteLine("Total bytes requested: " + iTotBytes.ToString());
		}
		
		/// <summary>
		/// Overloaded function, takes the string and coverts to bytes and calls
		/// </summary>
		/// <param name="sData">
		/// A <see cref="String"/>
		/// </param>
		/// <param name="sSocket">
		/// A <see cref="Socket"/>
		/// </param>
		private void SendToBrowser(String sData, ref Socket sSocket)
		{
			SendToBrowser (Encoding.ASCII.GetBytes(sData), ref sSocket);
		}
		
		/// <summary>
		/// Sends the data to the client (browser) 
		/// </summary>
		/// <param name="bSendData">
		/// A <see cref="Byte[]"/>
		/// </param>
		/// <param name="sSocket">
		/// A <see cref="Socket"/>
		/// </param>
		public void SendToBrowser(Byte[] bSendData, ref Socket sSocket)
		{
			int numBytes = 0;
			try
			{
				if(sSocket.Connected)
				{
					if((numBytes = sSocket.Send(bSendData,
					                            bSendData.Length,0)) == -1)
						Console.WriteLine("Socket Error! Cannot send packet!");
					else
					{
						Console.WriteLine("Number of bytes sent {0}" , numBytes);
					}
				}
				else
					Console.WriteLine("Connetion dropped...");
			}
			catch(Exception e)
			{
				Console.WriteLine("Error Occurred: {0} ", e );
			}
		}
		
		public static void Main()
		{
			WebServer  WS = new WebServer();
			// Hacky fix to stop warning CS0219
			WS.ToString();
		}
		
		
		public void StartListen()
		{
			int iStartPos = 0;
			String sRequest;
			String sDirName;
			String sRequestedFile;
			String sErrorMessage;
			String sLocalDir;
			String sPhysicalFilePath = "";
			String sFormattedMessage = "";
			String sResponse = "";
			
			while(true)
			{
				// Accept a new connection
				Socket sSocket = sListener.AcceptSocket();
				
				Console.WriteLine("\nSocket Type: " + sSocket.SocketType);
				if(sSocket.Connected)
				{
					Console.WriteLine("Client Connected\nClient IP {0}", sSocket.RemoteEndPoint);
					
					// Make a byte array and receive data from the client
					Byte[] bReceive = new Byte[1024];
					
					// Required for now to stop server locking up
					// Will cause an exception when request is not completed
					int i = sSocket.Receive(bReceive,bReceive.Length,0);
					
					// Convert byte to string
					string sBuffer = Encoding.ASCII.GetString(bReceive);
					
					// At present we will only deal with GET
					if(sBuffer.Substring(0,3) != "GET")
					{
						Console.WriteLine("Only GET method is supported.");
						sSocket.Close();
						return;
					}
					
					// Look for HTTP requests
					iStartPos = sBuffer.IndexOf("HTTP",1);
					
					// Get the HTTP text and version
					// e.g. It will return HTTP/1.1
					string sHTTPVersion = sBuffer.Substring(iStartPos,8);
					
					// Extract the requested type and requested file/directory
					sRequest = sBuffer.Substring(0,iStartPos - 1);
					
					// Replace backslash with forward slash - if any
					sRequest.Replace("\\","/");
					
					// If the file name is not supplied add forward slash to
					// indicate that it is a directory and then we will look for
					// default file name
					if((sRequest.IndexOf(".") < 1) && (!sRequest.EndsWith("/")))
					{
						sRequest = sRequest + "/";
					}
					// Extract the requested file name
					iStartPos = sRequest.LastIndexOf("/") + 1;
					sRequestedFile = sRequest.Substring(iStartPos);
					
					// Extract the directory name
					sDirName = sRequest.Substring(sRequest.IndexOf("/"),
					                              sRequest.LastIndexOf("/") - 3);
					
					if(sDirName == "/")
						sLocalDir = sWebServerRoot;
					else
					{
						// Get the Virutal Directory
						sLocalDir = GetLocalPath(sWebServerRoot, sDirName);
					}
					
					Console.WriteLine("Directory Requested: " + sLocalDir);
					
					// If the physical directory does not exists then
					// display the error message
					if(sLocalDir.Length == 0)
					{
						sErrorMessage = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd'>\r" + "<html xmlns='http://www.w3.org/1999/xhtml'>\n\n" + "<head>\r" + "<title>404 - Not Found</title>\r" + "<style type='text/css'>\r" + "div.foot { font: 90% monospace; color: #787878; padding-top: 4px;}\r" + "</style>\r" + "</head>\n\n" + "<body>\r" + "<h1>404 - Not Found</h1>\r" + "<div class='foot'>SharpServ/0.1</div>\r" + "</body>\r" + "</html>\r";
						
						// Format the message
						SendHeader(sHTTPVersion, "", sErrorMessage.Length, " 404 Not Found", ref sSocket);
						
						// Send to the browser
						SendToBrowser(sErrorMessage, ref sSocket);
						
						sSocket.Close();
						
						continue;
					}
					
					// Identify the file name
					// If the file name is not supplied then look in the default file list
					if(sRequestedFile.Length == 0)
					{
						// Get the default file name
						sRequestedFile = GetDefaultFileName(sLocalDir);						
						if(sRequestedFile == "")
						{
							sErrorMessage = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd'>\r" + "<html xmlns='http://www.w3.org/1999/xhtml'>\n\n" + "<head>\r" + "<title>500 - Internal Server Error</title>\r" + "<style type='text/css'>\r" + "div.foot { font: 90% monospace; color: #787878; padding-top: 4px;}\r" + "</style>\r" + "</head>\n\n" + "<body>\r" + "<h1>500 - Internal Server Error</h1>\r" + "<div class='foot'>SharpServ/0.1</div>\r" + "</body>\r" + "</html>\r";
							SendHeader(sHTTPVersion, "", sErrorMessage.Length,
							           "404 Not Found", ref sSocket);
							SendToBrowser(sErrorMessage, ref sSocket);
							
							sSocket.Close();
							return;
						}
					}
					
					// Get the MIME type
					String sMIMEType = GetMimeType(sRequestedFile);
					
					// Build the physical path
					sPhysicalFilePath = sLocalDir + sRequestedFile;
					Console.WriteLine("File Requested: " + sPhysicalFilePath);
					
					if(File.Exists(sPhysicalFilePath) == false)
					{
						sErrorMessage = "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd'>\r" + "<html xmlns='http://www.w3.org/1999/xhtml'>\n\n" + "<head>\r" + "<title>404 - Not Found</title>\r" + "<style type='text/css'>\r" + "div.foot { font: 90% monospace; color: #787878; padding-top: 4px;}\r" + "</style>\r" + "</head>\n\n" + "<body>\r" + "<h1>404 - Not Found</h1>\r" + "<div class='foot'>SharpServ/0.1</div>\r" + "</body>\r" + "</html>\r";
						SendHeader(sHTTPVersion, "", sErrorMessage.Length,
						           "404 Not Found", ref sSocket);
						SendToBrowser(sErrorMessage, ref sSocket);
						
						Console.WriteLine(sFormattedMessage);
					}
					else
					{
						int iToBytes=0;
						
						sResponse = "";
						
						FileStream fs = new FileStream(sPhysicalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
						
						// Create a reader that can read bytes from FileStream
						BinaryReader reader = new BinaryReader(fs);
						byte[] bytes = new byte[fs.Length];
						int read;
						while((read = reader.Read(bytes, 0, bytes.Length)) != 0)
						{
							// Read from the file and write the data to network
							sResponse = sResponse + Encoding.ASCII.GetString(bytes, 0, read);
							
							iToBytes = iToBytes + read;
						}
						reader.Close();
						fs.Close();
						
						SendHeader(sHTTPVersion, sMIMEType, iToBytes, " 200 OK", ref sSocket);
						SendToBrowser(bytes, ref sSocket);
						// sSocket.Send(bytes, bytes.Length, 0);
					}
					sSocket.Close();
				}
			}
		}
	}
}
