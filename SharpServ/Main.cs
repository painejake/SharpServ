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
	class MainClass
	{
		private TcpListener serverListener;

		// Port the web server will listen on. Will move this
		// into a XML configuration file at a later date
		private int port = 81;
		
		public MainClass()
		{
			try
			{
				// Start listening on selected port
				serverListener = new TcpListener(port);
				serverListener.Start();
				
				Console.WriteLine("SharpServ started successfully. Press ^C to stop...");
				Console.WriteLine("Listening on port: " + port);
				
				// Start the thread which casll the methods 'StartListen'
				Thread listenThread = new Thread(new ThreadStart(StartListen));
				listenThread.Start();
			}
			catch(Exception e)
			{
				Console.WriteLine("An exception occured while listening: " + e.ToString());
			}
		}
		
		public string GetDefaultFileName(string sLocalDirectory)
		{
			StreamReader sReader;
			String sLine = "";
			
			try
			{
				// Here we open Default.txt to find out
				// the default files to serve
				sReader = new StreamReader("data\\Default.txt");
				
				while ((sLine = sReader.ReadLine()) !=null)
				{
					// Look for default file in the web server
					// root folder
					if (FileWebRequest.Exists(sLocalDirectory + sLine) == true)
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
			
			try
			{
				// Here we open the Vdir.txt to find out
				// the list of virtual directories
				sReader = new StreamReader("data\\Vdir.txt");
				
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
			
			if(sVirtualDir == sDirName)
				return sActualDir;
			else
				return "";
		}
		
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
				sReader = new StreamReader("data\\Mime.txt");
				
				while((sLine = sReadLine()) !=null)
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
			sBuffer = sBuffer + "Server: SharpServ-b\r\n";
			sBuffer = sBuffer + "Content-Type: " + sMIMEHeader + "\r\n";
			sBuffer = sBuffer + "Accept-Ranges: bytes\r\n";
			sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n\r\n";
			
			Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
			
			SendToBrowser(bSendData, ref sSocket);
			
			Console.WriteLine("Total Bytes: " + iTotBytes.ToString());
		}
		
		private void SendToBrowser(String sData, ref Socket sSocket)
		{
			SendToBrowser (Encoding.ASCII.GetBytes(sData), ref sSocket);
		}
		
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
						Console.WriteLine("No. of bytes send {0}" , numBytes);
					}
				}
				else
					Console.WriteLine("Connetion dropped...");
			}
			catch(Exception e)
			{
				Console.WriteLine("Error Occurred : {0} ", e );
			}
		}
		
		public void StartListen()
		{
			int iStartPos = 0;
			String sRequest;
			String sDirName;
			String sRequestedFile;
			String sErrorMessage;
			String sLocalDir;
			String sWebServerRoot = "\\www\\"; 	// Web server root set here
			String sPhysicalFilePath = "";		// will be moved to XML config
			String sFormattedMessage = "";
			String sResponse = "";
			
			while(true)
			{
				// Accept a new connection
				Socket sSocket = serverListener.AcceptSocket();
				
				Console.WriteLine("Socket Type " + sSocket.SocketType);
				if(sSocket.Connected)
				{
					Console.WriteLine("\nClient Connected!\n==================\nClient IP {0}\n", sSocket.RemoteEndPoint);
					
					// Make a byte array and receive data from the client
					Byte[] bReceive = new Byte[1024];
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
					
					// Identify the file name
					// If the file name is not supplied then look in the default file list
					if(sRequestedFile.Length == 0)
					{
						// Get the default file name
						sRequestedFile = GetTheDefaultFileName(sLocalDir);
						
						if(sRequestedFile == "")
						{
							sErrorMessage = "<h2>Oh Dear! No default file name specified</h2>";
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
						sErrorMessage = "<h2>404 Error! File does not exist.</h2>";
						SendHeader(sHTTPVersion, "", sErrorMessage.Length,
						           "404 Not Found", ref sSocket);
						SendToBrowser(sErrorMessage, ref sSocket);
						
						Console.WriteLine(sFormattedMessage);
					}
					else
					{
						int iToBytes=0;
						
						sResponse = "";
						
						FileStream fs= new FileStream(sPhysicalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
						
						// Create a reader that can read bytes from FileStream
						BinaryReader reader = new BinaryReader(fs);
						byte[] bytes = new byte(fs.Length);
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
