using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

//[ExecuteInEditMode] // Change FixedUpdate to Update if you do this
/**
 * Nexus_Listener extracts double precision values from a binary file
 * exported by the software BioTrace+ by MindMedia (Neuro and Biofeedback Systems)
 * The software can draw data from connected NeXuS devices or from previously
 * recorded sessions.
 */
public class Nexus_Listener : MonoBehaviour {

	#region Constants declarations
	public const int ChannelEDA = 5;
	public const int ChannelBVP = 7;
	public const int ChannelRespiration = 8;
	public const int ChannelBVPAmp = 22;
	public const int ChannelHeartRate = 23;
	#endregion

	#region Inspector configurable variables
	[Header("NeXus poll settings")]
	[Tooltip("Activate output of realtime data in the BioTrace+ system settings and enter the path here")]
	[SerializeField]
	private string filepath = "C:\\BioTrace+ NX10\\System\\DataChan.bin"; // Configure in inspector

	[Tooltip("How often per second we check for new sensor data (best use multiples of 2)")]
	[Range(1, 2048)]
	[SerializeField]
	private int pollsPerSecond = 64; // Configure in inspector
	[Tooltip("This shows how many updates we had in the last second")]
	public int debugCurrentPPS = 0;

	/* Some default channel configurations from NX10-Basic / Validate before using
	 * 5: Sensor-E:EDA (Hautleitwert)
	 * 7: Sensor-G:BVP
	 * 8: Sensor-H:Atmung
	 * 22: [G] BVP Amp.
	 * 23: [G] Heart Rate
	*/
	[Tooltip("Set your channels like they are configured in BioTrace+. Open the software and press c. Don't change this here on runtime. Channel numbers start at 1")]
	[SerializeField]
	private int[] channelNumbersToRead = new int[] {ChannelEDA, ChannelBVP, ChannelRespiration, ChannelBVPAmp, ChannelHeartRate};
	[Tooltip("If the receiving software cannot handle double precision values activate this to send data as floating point values")]
	[SerializeField]
	private bool convertDoubleToFloat = false;
	[SerializeField] // Show in inspector for debugging
	private object[] channel;
	#endregion

	#region Private variable declarations
	private FileStream stream;
	private BinaryReader binReader;


	private int counter = 0;
	private float oneSecondTimestamp = 0;
	#endregion

	#region MonoBehaviour methods (Start, Update, ...)
	protected void Awake() {
		SetUpdateRate(pollsPerSecond);
	}

	protected void Start() {

		int channelNumberMax = 0;
		foreach (int channelNumber in channelNumbersToRead) {
			channelNumberMax = Math.Max(channelNumber, channelNumberMax);
		}
		channel = new object[channelNumbersToRead.Length];
	}

	static double[] currentValue = new double[16];
	static double oldValue = 0;

	protected void FixedUpdate() {
		// Count polls (/frames) per second if in editor
		if (Application.isEditor) {
			counter++;
			if ((Time.time - 1) >= oneSecondTimestamp) {
				debugCurrentPPS = counter;
				counter = 0;
				oneSecondTimestamp = Time.time;
			}
		}

		// Abort if array lengths mismatch
		if (channel.Length != channelNumbersToRead.Length) {
			Debug.LogError("Number of channel slots is different than number of channels to read. Do not change these arrays on runtime");
			return;
		}

		// Read data from nexus biotrace+ file
		//Todo: Catch DirectoryNotFoundException
		stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		binReader = new BinaryReader(stream);
		try {
			int channelNumber;
			for (int i = 0; i < channelNumbersToRead.Length; i++) {
				channelNumber = channelNumbersToRead[i] - 1; // Actual channel numbers start with 1
				binReader.BaseStream.Position = 8 * channelNumber;
				try {
					double d = binReader.ReadDouble();
					if (convertDoubleToFloat) {
						channel.SetValue((float)d, i);
					} else {
						channel.SetValue(d, i);
						
					}

				} catch (IndexOutOfRangeException e) {
					Debug.Log("IOOB: channelNumber: " + channelNumber + " Error: " + e.ToString());
				}
			}
		} catch (EndOfStreamException e) {
			Debug.Log("Nexus_Listener: Error writing data:" + e.ToString());
			Console.WriteLine("Nexus_Listener: Error writing data: {0}.",
				e.GetType().Name);
		} finally {
			binReader.Close();
			stream.Close(); //Done by the BinaryReader.Close() already!?!?
		}

		// Send data via OSC
		string logMessage = "Channel values: ";
		for (int i = 0; i < channel.Length; i++) {
			logMessage += $"Channel {channelNumbersToRead[i]}: {channel[i]}, ";
		}

		double currentSum = 0;
		bool didIt = false;

		for (int i = 0; i < currentValue.Length-2; i++) {
			if (currentValue[i] == 0) {
				currentValue[i] = (Double)channel[0];
				currentValue[15] = i;
				didIt = true;
				break;
			}
		}

		if (didIt == false) {
			currentValue[(int)currentValue[15]] = (Double)channel[0];
			if (currentValue[15] < 14) {
				currentValue[15] = currentValue[15] + 1;
			}
			else if (currentValue[15] == 14) {
				currentValue[15] = 0;
			}
		}

		for (int i = 0; i < currentValue.Length - 1; i++) {
    		currentSum += currentValue[i];
		}

		double smoothValue = currentSum / currentValue.Length-1;
		public int breathe = 0;

		if (oldValue != 0) {
    		if (oldValue < smoothValue) {
        		breathe = 1;
    		} else if (oldValue == smoothValue) {
        		breathe = 0;
    		} else {
        		breathe = -1;
    		}
		} else {
    		breathe = 0;
		}
		oldValue = smoothValue;
	}

	protected void OnValidate() {
		SetUpdateRate(pollsPerSecond);
	}
	#endregion

	private void SetUpdateRate(int valuesPerSecond) {
		// Set the approximate poll rate for updates by changing the waiting time between
		//  FixedUpdate() calls;
		Time.fixedDeltaTime = 1f / valuesPerSecond;
	}
}
