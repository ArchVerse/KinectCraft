package net.minecraft.src;

import java.net.*;
import java.io.*;
import java.util.*;

public class WoollySocks extends Thread {

	static final int PORT = 9001;
	ServerSocket serverSocket = null;
	public boolean isListening = true;
	BufferedReader br;
	String messageIn;
	public String message;
	public List<NewMessage> messageCallbacks = new ArrayList<NewMessage>();
	private PrintWriter mOut;
	
	public interface NewMessage{
		void sendMessage(String message);
		void connected();
	}
	
	public WoollySocks() {
		 
		try 
		{
			serverSocket = new ServerSocket(PORT);
		       
		    System.out.println("Server started on port " + PORT);
		}		    
		
		catch (IOException se) 
		{
			System.err.println("Can not start listening on port " + PORT);
		    se.printStackTrace();
		    System.exit(-1);
		}
		
		this.start();
	}
	
	public void sendMessage(String message)
    {
         String encoded;
        try {
            encoded = URLEncoder.encode(message,"UTF-8");
            mOut.println(encoded);
            mOut.flush();
        } catch (UnsupportedEncodingException e) {
            e.printStackTrace();
        }

    }
	
	public void run()
	{
		try {
			Socket socket = serverSocket.accept();
			br = new BufferedReader(new InputStreamReader(socket.getInputStream()));
			System.out.println("Connection recieved on port: " + PORT);
			
			if(!messageCallbacks.isEmpty())
			{
				for(int i = 0; i < messageCallbacks.size(); i++)
				{
					NewMessage messageCallback = messageCallbacks.get(i);
					if(messageCallback != null)
						messageCallback.connected();
				}
			}
			
			while(isListening)
			{
				try
				{
					messageIn = br.readLine();
				}
				catch (Exception e)
				{
					socket = serverSocket.accept();
					br = new BufferedReader(new InputStreamReader(socket.getInputStream()));
					System.out.println("Connection recieved on port: " + PORT);
				}
				if(messageIn != null)
				{
					message = URLDecoder.decode(messageIn, "UTF-8");
					if(!messageCallbacks.isEmpty())
					{
						for(int i = 0; i < messageCallbacks.size(); i++)
						{

		            		System.out.println("Message Recieved: " + message);
							NewMessage messageCallback = messageCallbacks.get(i);
							if(messageCallback != null)
								messageCallback.sendMessage(message);
						}
					}
				}
			}
		} catch (IOException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}
	
	
}
