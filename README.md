# Mola

_This is a students project for university._

This is a simple VR game written in Unity.
The goal of this project is to increase the immersion of the player in the VR environment by simulating the players breathing within the game. 

## Installation and Usage

### **without** simulated breathing
To install the game, download / clone this repository onto your pc.
Then import the entire folder into Unity as a project. Open the project, plug in your VR headset and run the game.

### **with** simulated breathing
_longer german explanation below_

Proceed as described in _without breathing simulated_, but before running the game, connect a Nexus 10 breathing sensor to Unity.
For the Nexus 10, first install the corresponding application from the official website
> https://mindmedia.freshdesk.com/support/solutions/articles/36000097057-biotrace-software-downloads

After installing the software, connect your Nexus 10 and run the software in administrator mode.
Then go to _Configuration > System Settings_ and enable _"output live data in [your path]"_.
Copy that path for Unity for later.
Now head to _Signal Library > Respiration > Resp Basic_ to monitor your breathing.
Create a new User/Client and test if your Nexus 10 is working properly.

Once you made sure your Nexus 10 is working as intended, leave BioTrace+ open (or running) and open Unity.
You now need to change the settings of the NexusListener to your PCs path of the Nexus 10 binary file.
Locate the listener under:
> Main_new > SharkCage_Sketchfab > XRPlayer > AutoHandPlayer > Particle jiggly bubble underwater exhailing continuously

The NexusListener is a component of that particle emitter.
You need to paste your specific path of the Nexus 10 binary file into the Filepath.
Make sure the _Channel Numbers to Read_ is set to 1 and the Channel (_Element 0_) is set to 8.

Now all you need to do is start the breathing monitoring in the BioTrace software and then run the game.

## Contributing

This is a students project for university.
The project has allready been finished.



## german instructions
#### Nexus 10 Tracking
Anleitung
1. Anwendung installieren
   * https://mindmedia.freshdesk.com/support/solutions/articles/36000097057-biotrace-software-downloads
   * Vollversion auswählen
   * Anwendung als Administrator starten
2. Setup in BioTrace+
   * Live Output aktivieren
      + Konfiguration/Systemeinstellungen/Echtzeitdaten aktivieren
      + in Installationsordner
   * Signalbibliothek/Atmung
      + neuer Nutzer
      + Gerät Port H
      + zum Starten der Aufnahme auf den roten Kreis
   * Nexus Anleitung
      + Gerät auspacken
      + Gerrät anschalten (über roten Button)
      + per USB verbinden (Display zeigt Verbindung an)
      + Atemsensor an Port H anschließen
3. Unity Setup
   * nach Script suchen: Nexus_DataReader
   * File Path zu Speicherort der .bin änndern
