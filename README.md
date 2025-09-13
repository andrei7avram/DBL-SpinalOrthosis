# 🦴 DBL-SpinalOrthosis: Real-Time Posture Digital Twin

> **Digital Twin of a Spinal Orthosis Vest with Reinforcement Learning Posture Detection**  
> 💡 *Developed as part of the DBL "Digital Twins of Medical Devices" course at Eindhoven University of Technology.*

---

## 🩻 Overview

**DBL-SpinalOrthosis** is an innovative real-time **digital twin system** that interfaces with a custom-built **wearable spinal orthosis**. This wearable uses **stretch sensors** to capture spinal posture data, which is transmitted via **Bluetooth** to a digital human model in **Unity**. Using **reinforcement learning**, the digital twin replicates the user’s posture in 3D, identifies bad postures (like scoliosis, kyphosis, etc.), and delivers feedback directly to the user.

This system is aimed at **posture correction and spinal health monitoring**, addressing the growing need for **real-time feedback** in rehabilitation and everyday posture maintenance.

---
<img width="1011" height="565" alt="image" src="https://github.com/user-attachments/assets/0cbb4e79-f4b7-4d67-aac1-9ce14f1f30e0" />

---

## 🚩 Problem Statement

> 💡 **80% of the population experiences posture problems.**

Existing spinal orthosis solutions often:
- Lack **real-time posture feedback**
- Don’t provide **personalized correction**
- Offer **limited user interaction**

This project bridges the **physical and digital domains** to address those gaps through digital twinning, machine learning, and embedded systems.

---

## 🧠 How It Works

### 1. **Hardware (The Vest)**
- **8 custom stretch sensors** placed across:
  - Cervical region (neck)
  - Thoracic and lumbar back
  - Shoulder blades
- Sensors track **bending**, **rotation**, and **tilting** of the spine
- Data transmitted via **Bluetooth** from an **ESP32 microcontroller**

### 2. **Software (The Digital Twin)**
- Developed in **Unity**
- A **1:1 humanoid model** simulates bone movements
- Reinforcement Learning agent trained to:
  - Receive sensor values (observations)
  - Adjust bone rotations (actions)
  - Receive feedback (rewards) based on how closely it mimics the user's posture

### 3. **Feedback System**
- When poor posture is detected:
  - It is **visualized in real-time**
  - A **notification** is sent to the user’s phone
  - Enables **self-correction** or clinical intervention

---

## 🤖 Why Reinforcement Learning?

- 8 continuous sensor inputs create a **complex, high-dimensional state space**
- Traditional mapping is ineffective
- A trained RL agent can **intuitively understand** how to position the bones to match sensor input
- The result is a **robust, generalizable model** that adapts to various body types and postures

---

## 🛠️ Tech Stack

| Component | Technology |
|----------|-------------|
| Microcontroller | ESP32 |
| Sensors | Custom strain/stretch sensors |
| Connectivity | Bluetooth |
| Simulation | Unity |
| ML Model | Reinforcement Learning (custom) |
| Platform | Android/iOS compatible |

---

## 🔧 Features

- ✅ Real-time 3D spinal posture reconstruction
- ✅ Digital twin powered by reinforcement learning
- ✅ Bluetooth communication between vest and phone
- ✅ Wearable, power-efficient, and customizable hardware
- ✅ Detects scoliosis, kyphosis, lordosis, forward head posture
- ✅ Gender- and size-adaptive model calibration

---

## 🔍 Future Improvements

- 🔬 Integration of **higher-quality sensors**
- 📊 Real-life posture **data collection for better training**
- 📏 **Personalized 3D models** based on user dimensions

---

## ♻️ Sustainability & Usability

- 🔄 Replaceable sensors for **repairability**
- 🔋 Low-power design for **longer battery life**
- 📱 Uses user's **smartphone for computation** to reduce cost
- 🌿 Made from **breathable organic materials**
