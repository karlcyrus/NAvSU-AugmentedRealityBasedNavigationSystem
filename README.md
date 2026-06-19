<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3.62f3-000000?style=for-the-badge&logo=unity&logoColor=white" />
  <img src="https://img.shields.io/badge/AR%20Foundation-5.x-00C853?style=for-the-badge&logo=unity&logoColor=white" />
  <img src="https://img.shields.io/badge/Next.js-14-000000?style=for-the-badge&logo=next.js&logoColor=white" />
  <img src="https://img.shields.io/badge/MySQL-8.0-4479A1?style=for-the-badge&logo=mysql&logoColor=white" />
  <img src="https://img.shields.io/badge/Platform-Android-3DDC84?style=for-the-badge&logo=android&logoColor=white" />
</p>

# 📍 NAvSU — Augmented Reality-Based Campus Navigation System Utilizing Dijkstra's Algorithm

**NAvSU** is an Android-based augmented reality (AR) navigation application designed to help students, visitors, and faculty navigate a university campus in real time. The app overlays directional AR markers onto the real world through the device's camera, guiding users along the **shortest path** to their selected destination using **Dijkstra's Algorithm**.

> 🖥️ The **Admin Dashboard** (built with Next.js) is maintained in a separate repository: [campus-ar-admin](https://github.com/karlcyrus/campus-ar-admin)

---

## ✨ Features

- 🗺️ **AR Navigation** — Real-time augmented reality directional arrows overlaid on the camera view to guide users to their destination.
- 🧮 **Dijkstra's Shortest Path Algorithm** — Computes the optimal route between the user's current location and the selected building.
- 📱 **Mobile-First (Android)** — Built as a native Android app using Unity and AR Foundation.
- 🏫 **Campus Building Directory** — Browse or search for campus buildings with images and descriptions.
- 🔐 **Admin Dashboard** — A web-based admin panel built with Next.js for managing campus data, building information, and system settings.
- 📡 **Real-Time Surface Detection** — Uses ARCore to detect real-world surfaces for accurate AR placement.

---

## 🛠️ Tech Stack

| Component | Technology |
|---|---|
| **Mobile App** | Unity 2022.3.62f3 (LTS) |
| **AR Framework** | AR Foundation + ARCore |
| **Language (App)** | C# |
| **Pathfinding** | Dijkstra's Algorithm |
| **Admin Dashboard** | Next.js 14, React 18 |
| **Database** | MySQL 8.0 |
| **Authentication** | bcrypt.js |
| **Platform** | Android |

---

## 📁 Project Structure

```
NAvSU/
├── Assets/                     # Unity assets
│   ├── Scripts/                # C# scripts (AR logic, pathfinding, UI controllers)
│   ├── Scenes/                 # Unity scenes
│   ├── Models/                 # 3D models and characters
│   ├── Prefabs/                # Reusable Unity prefabs
│   ├── CampusBuildingImages/   # Campus building photos
│   ├── Fonts/                  # Custom fonts and TextMeshPro assets
│   └── Resources/              # Campus map and other runtime resources
├── Packages/                   # Unity package manifest
├── ProjectSettings/            # Unity project configuration
├── .gitignore
└── .vsconfig
```

---

## 🚀 Getting Started

### Prerequisites

- **Unity Hub** with **Unity 2022.3.62f3** (including Android Build Support module)
- **Android device** with ARCore support (for testing the mobile app)

---

### 📱 Mobile App Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/karlcyrus/NAvSU-AugmentedRealityBasedNavigationSystem.git
   ```
2. Open **Unity Hub** → Click **Open** → Select the cloned project folder.
3. Unity will auto-detect the required version (`2022.3.62f3`). Install it if prompted.
4. On first launch, Unity will regenerate the `Library` folder automatically. This may take a few minutes.
5. To build the APK: Go to **File → Build Settings → Android → Build**.

---

### 📲 Installing the APK

1. Transfer the `.apk` file to your Android device.
2. Enable **Install from Unknown Sources** in your device settings.
3. Tap the `.apk` file to install.
4. Grant **Camera** and **Location** permissions when prompted.

---

## 📖 How to Use

1. **Open** the NAvSU app on your Android device.
2. **Browse or search** for your desired campus building.
3. **Select** your destination.
4. **Point your camera** at the ground or surroundings — the app will detect surfaces using AR.
5. **Follow the AR arrows** overlaid on your screen to navigate to your destination.
6. The app calculates the **shortest path** in real time using Dijkstra's Algorithm.

---

## 👥 Authors

- **Karl Cyrus S. Geron**
- **Jhon Rhey G. Valleramos**
- **Antonio Miguel V. Villafiania**

---

## 📄 License

This project was developed as a capstone/thesis requirement. All rights reserved.

---

<p align="center">
  <sub>Built with 💚 using Unity, AR Foundation, and Next.js</sub>
</p>
