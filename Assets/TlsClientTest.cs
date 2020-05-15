using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.IO;
using System.Net;
using System;
using System.Net.Sockets;
using System.Net.Security;
using System.Linq;

public class TlsClientTest : MonoBehaviour
{

	class State
	{
		public bool done;
		public string text;
		public Exception failure;
	};


	// Does a prefix,depth-first traversal through an Exceptions InnerException.
	// Includes special treatment for AggregateException which has many children.
	private IEnumerable<System.Exception> ExpandException (System.Exception x)
	{
		yield return x;
		AggregateException a = x as AggregateException;
		if (a != null) {
			foreach (var inner in a.InnerExceptions)
				foreach (Exception child in ExpandException(inner))
					yield return child;
		}
		else if (x.InnerException != null)
			foreach (Exception child in ExpandException (x.InnerException))
				yield return child;		
	}



	// Start is called before the first frame update
	IEnumerator Start()
	{
		State s = new State();
		ThreadPool.QueueUserWorkItem (DoClient, s);
		while (!s.done)
			yield return null;
		if (s.failure == null)
			Debug.Log("Succeeded: {s.text}");
		else
		{
			IEnumerable<Exception> exceptions = ExpandException(s.failure);
			string combined = string.Join("\n", exceptions.Select(x => $"{x.GetType()}: {x.Message}\n").ToArray());
			Debug.Log($"Encountered Exceptions: {combined}");
		}
	}


	private static void DoClient(object o)
	{
		State state = (State)o;
		try
		{
			string host = "self-signed.badssl.com";
			var client = new TcpClient(host, 443);
			SslStream ssl = new SslStream(client.GetStream());
			ssl.AuthenticateAsClient(host);
			StreamWriter w = new StreamWriter(ssl);
			w.Write($"HEAD / HTTP/1.1\r\nHost: {host}\r\n\r\n");
			w.Flush();
			StreamReader r = new StreamReader(ssl);
			StringWriter result = new StringWriter();
			while (true)
			{
				string line = r.ReadLine();
				if (string.IsNullOrEmpty(line))
					break;
				result.WriteLine(line);
			}
			client.Close();
			state.text = result.ToString();
		}
		catch (Exception x)
		{
			state.failure = x;
		}
		state.done = true;

	}


}
