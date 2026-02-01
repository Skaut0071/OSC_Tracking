# 🛠️ OSC Tracking for SlimeVR

**A lightweight Unity-based solution to simulate VR tracking without SteamVR.**

This application allows you to send tracking data (Head and two Hands/Controllers) directly to the [SlimeVR Server](https://github.com/SlimeVR/SlimeVR-Server) via OSC. It was specifically designed to demonstrate SlimeVR capabilities on hardware that isn't VR-ready (like non-gaming laptops) while maintaining full situational awareness using **Passthrough**.

---

## 🚀 Key Features

* **No SteamVR Required:** Bypass the heavy SteamVR/OpenXR runtimes. Ideal for low-end PCs or quick demonstrations.
* **3-Point Tracking:** Simulates a virtual skeleton using the Headset and Controllers/Hands as trackers.
* **Passthrough AR:** See your real-world environment while the virtual skeleton moves in the SlimeVR Server.
* **Standalone Ready:** Built for Meta Quest, allowing for a portable and "cable-free" demonstration setup.

---

## 📖 How It Works

The app captures the world-space coordinates of your Quest headset and controllers within Unity. It then packages this data into **OSC (Open Sound Control)** packets and sends them over UDP to your PC. 

SlimeVR Server receives these as external trackers, allowing you to visualize a full-body skeleton or test proportions without ever launching a VR game.

---

## 🛠️ Setup Instructions

### 1. SlimeVR Server Configuration
1.  Open your **SlimeVR Server**.
2.  Ensure the **VRChat OSC Trackers** is enabled in the settings.
3.  Set the port in to `9001` (default).

### 2. App Configuration
1.  Launch the app on your Headset.
2.  The app should automaticly pickup your SlimeVR Server IP address.
3.  Accept the **Spatial Data/Passthrough** permissions if prompted.

---

## 📱 Tested Devices
* **Meta Quest 3** (Full support for Passthrough and Controller/Hand tracking)

---

## 🏗️ Technical Specifications
* **Engine:** Unity 2022.3 LTS
* **Protocol:** UDP via OSC
* **Default Port:** 9001
* **Data Sent:** Position and Rotation for `head`, `left_hand`, and `right_hand`.

---

## ⚠️ Disclaimer
This is an open-source tool meant for testing and demonstration purposes. It does not replace a full VR runtime for gaming but serves as a bridge for SlimeVR visualization.
