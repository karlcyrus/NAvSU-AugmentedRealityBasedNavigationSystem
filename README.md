<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3.62f3-000000?style=for-the-badge&logo=unity&logoColor=white" />
  <img src="https://img.shields.io/badge/Niantic%20Lightship-WPS-7B2FF7?style=for-the-badge&logo=niantic&logoColor=white" />
  <img src="https://img.shields.io/badge/AR%20Foundation-5.x-00C853?style=for-the-badge&logo=unity&logoColor=white" />
  <img src="https://img.shields.io/badge/Next.js-14-000000?style=for-the-badge&logo=next.js&logoColor=white" />
  <img src="https://img.shields.io/badge/MySQL-8.0-4479A1?style=for-the-badge&logo=mysql&logoColor=white" />
  <img src="https://img.shields.io/badge/Platform-Android-3DDC84?style=for-the-badge&logo=android&logoColor=white" />
</p>

# 📍 NAvSU — Augmented Reality-Based Campus Navigation System Utilizing Dijkstra's Algorithm

**NAvSU** is an Android-based augmented reality (AR) campus navigation system that combines **Niantic Lightship's World Positioning System (WPS)**, **real-time GPS tracking**, and **Dijkstra's shortest path algorithm** to guide users across a university campus. The system constructs a **weighted navigation graph** of geo-referenced campus nodes and edges, computes the optimal route using graph-theoretic pathfinding, then renders directional AR markers anchored to real-world GPS coordinates through the device's camera — enabling intuitive, turn-by-turn outdoor navigation in augmented reality.

> 🖥️ The **Admin Dashboard** (built with Next.js) is maintained in a separate repository: [campus-ar-admin](https://github.com/karlcyrus/campus-ar-admin)

---

## ⚙️ How It Works — Technical Overview

### 🌐 Localization Pipeline

NAvSU uses a **three-gate localization system** that must all pass simultaneously before AR navigation begins:

1. **Niantic Lightship WPS (World Positioning System)** — Provides centimeter-level world positioning by fusing visual and satellite data. The `ARWorldPositioningManager` reports a `WorldPositioningStatus.Available` state once the device is accurately localized in world space. A **background warmup** runs silently on app launch to prime the WPS pipeline before the user selects a destination.

2. **GPS (Global Positioning System)** — The device's `LocationService` provides real-time latitude/longitude coordinates with a configurable accuracy threshold (default: **≤ 8 meters** horizontal accuracy). GPS is initialized with high-frequency updates (`1s` desired accuracy, `0.5m` distance filter).

3. **Stability Timer** — All three conditions (WPS available, GPS running, accuracy within threshold) must hold continuously for a configurable duration (default: **10 seconds**) before the navigation path is spawned, preventing jitter from transient GPS spikes.

### 🧮 Pathfinding — Dijkstra's Algorithm

The campus is modeled as a **weighted undirected graph** stored in `nav_graph.json`:

- **Nodes** — GPS-anchored waypoints across campus, each defined by a unique ID and `(latitude, longitude)` coordinates. Nodes are typed (e.g., waypoint, building entrance) and can reference Points of Interest (POI).
- **Edges** — Weighted connections between nodes, where the weight (`distance_m`) represents the walking distance in meters. Edges support bidirectional traversal.

At navigation start, the system:
1. Reads the user's **live GPS position** `(lat, lng)`.
2. Finds the **nearest graph node** by computing the Haversine distance to every node.
3. Runs **Dijkstra's algorithm** using a priority queue (`SortedList`) to find the shortest path to the destination node.
4. Returns the ordered sequence of node IDs representing the optimal route.

### 📐 Mathematical Functions

**Haversine Formula** — Computes the great-circle distance (in meters) between two GPS coordinates on Earth's surface:

$$d = 2R \cdot \arcsin\left(\sqrt{\sin^2\left(\frac{\Delta\phi}{2}\right) + \cos(\phi_1)\cos(\phi_2)\sin^2\left(\frac{\Delta\lambda}{2}\right)}\right)$$

Where $R = 6{,}371{,}000\text{m}$ (Earth's mean radius), $\phi$ = latitude, $\lambda$ = longitude.

**Bearing Computation** — Calculates the compass heading (0° = North, 90° = East) from point A to point B for orienting AR arrows:

$$\theta = \text{atan2}\left(\sin(\Delta\lambda)\cos(\phi_2),\ \cos(\phi_1)\sin(\phi_2) - \sin(\phi_1)\cos(\phi_2)\cos(\Delta\lambda)\right)$$

### 🏹 AR Object Spawning

Once the path is computed, the system spawns three types of AR objects anchored to GPS coordinates via Lightship's `ARWorldPositioningObjectHelper`:

| Object | Placement | Purpose |
|---|---|---|
| **Node Markers** | At each graph node along the path | Marks waypoints and decision points |
| **Interval Arrows** | Every 8m between consecutive nodes (linear interpolation) | Provides continuous directional guidance |
| **Destination Marker** | At the final node | Signals arrival at the destination |

Each object is rotated to face the next node using the computed bearing, and a **distance-based fade system** using the Haversine formula dynamically adjusts object opacity — fully visible within 25m, fully invisible beyond 35m — to reduce visual clutter and save GPU resources.

### 🧭 Real-Time Guidance

During active navigation, the system continuously:
- Tracks the user's GPS position
- Identifies the nearest path node
- Updates a **guidance bearing** pointing toward the next node
- Feeds this data to the **minimap** and **navigation UI card** for real-time ETA and distance display

---

## ✨ Features

- 🗺️ **AR Navigation** — Real-time augmented reality directional arrows overlaid on the camera view, geo-anchored via Lightship WPS.
- 🧮 **Dijkstra's Shortest Path** — Computes the optimal route across a weighted campus navigation graph.
- 📡 **GPS + WPS Fusion** — Combines satellite positioning with Niantic's visual localization for accurate outdoor AR placement.
- 📐 **Haversine Distance** — Calculates real-world distances between GPS coordinates on Earth's curved surface.
- 🧭 **Compass Bearing** — Orients AR arrows using forward azimuth computation between node pairs.
- 👁️ **Distance-Based Fading** — Dynamically hides far-away AR objects based on Haversine distance for performance and clarity.
- 🏫 **Campus Building Directory** — Browse or search for campus buildings with images and descriptions.
- 🤖 **AR Guide Companion** — An optional animated 3D character that accompanies the user during navigation.
- 🗺️ **Live Minimap** — Displays the computed path and user position in real time.
- 🔐 **Admin Dashboard** — A web-based admin panel (Next.js) for managing campus data, offices, and destinations via REST API.
- 📡 **Live Data Sync** — Office/destination data is fetched from a Railway-hosted API with local JSON fallback for offline resilience.

---

## 🛠️ Tech Stack

| Component | Technology |
|---|---|
| **Game Engine** | Unity 2022.3.62f3 (LTS) |
| **AR Framework** | AR Foundation + ARCore |
| **World Positioning** | Niantic Lightship ARDK — WPS (`ARWorldPositioningManager`) |
| **Language (App)** | C# |
| **Pathfinding** | Dijkstra's Algorithm (priority queue implementation) |
| **Geolocation Math** | Haversine formula, Forward Azimuth bearing |
| **Admin Dashboard** | Next.js 14, React 18 |
| **Database** | MySQL 8.0 |
| **API Hosting** | Railway |
| **Authentication** | bcrypt.js |
| **Platform** | Android (ARCore-compatible devices) |

---

## 📁 Project Structure

```
NAvSU/
├── Assets/                     # Unity assets
│   ├── *.cs                    # C# scripts (AR logic, pathfinding, UI controllers)
│   ├── Scenes/                 # Unity scenes
│   ├── Models/                 # 3D models (guide companion, etc.)
│   ├── Prefabs/                # AR arrow, node marker, destination marker prefabs
│   ├── CampusBuildingImages/   # Campus building photos
│   ├── Fonts/                  # Custom fonts and TextMeshPro assets
│   └── Resources/              # nav_graph.json, office_mapping.json, campus map
├── Packages/                   # Unity package manifest (Lightship, AR Foundation, etc.)
├── ProjectSettings/            # Unity project configuration
├── .gitignore
└── .vsconfig
```

### Key Files

| File | Description |
|---|---|
| `PreplaceWorldObjects.cs` | Core navigation manager — graph loading, Dijkstra's algorithm, GPS tracking, AR object spawning, Haversine/bearing math, WPS integration |
| `ARWarmupManager.cs` | Manages the localization warmup UI and stability progress |
| `LocationGateManager.cs` | Enforces GPS permission gate before allowing app usage |
| `MinimapController.cs` | Renders the live minimap with path and user position |
| `ArrivalPanelController.cs` | Handles arrival detection and destination reached UI |
| `ARGuideCompanion.cs` | Controls the animated 3D guide character behavior |
| `nav_graph.json` | Weighted navigation graph (nodes with GPS coords + edges with distances) |
| `office_mapping.json` | Maps office/building IDs to graph nodes and display names |

---

## 🚀 Getting Started

### Prerequisites

- **Unity Hub** with **Unity 2022.3.62f3** (including Android Build Support module)
- **Niantic Lightship ARDK** (included in Packages)
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
2. **Wait** for the localization warmup to complete (GPS + WPS stabilization).
3. **Browse or search** for your desired campus building/office.
4. **Select** your destination.
5. **Point your camera** at the surroundings — AR arrows will appear anchored to real-world positions.
6. **Follow the AR arrows** on your screen to navigate to your destination.
7. The app calculates the **shortest path** in real time using Dijkstra's Algorithm and displays your **ETA** and **remaining distance** on the navigation card.

---

## 👥 Authors

- **Karl Cyrus Celda**
- **Jhon Rhey G. Valleramos**
- **Antonio Miguel V. Villafiania**

---

## 📄 License

This project was developed as a capstone/thesis requirement. All rights reserved.

---

<p align="center">
  <sub>Built with 💚 using Unity, Niantic Lightship, AR Foundation, and Next.js</sub>
</p>
