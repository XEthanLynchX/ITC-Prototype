# ItcVue Project Overview

## What Is ITC?

Interoperable Train Control (ITC) is part of the communications infrastructure used by Positive Train Control (PTC), a railroad safety system.

The ITC communications network uses 220 MHz radio links to exchange data among several major parts of the PTC system:

- Wayside equipment located along the railroad
- Locomotives
- Railroad back-office systems
- The communications infrastructure connecting them

Wayside Interface Units (WIUs) collect information from railroad signal systems and transmit status messages over the ITC radio network. These messages can include information related to signal aspects, switch positions, and other wayside conditions.

## What Is ITCMon?

ITCMon is community-developed software that receives and decodes supported ITC radio messages.

A typical receiver site uses:

1. An antenna and software-defined radio to receive 220 MHz transmissions
2. Radio-processing software to demodulate the signal
3. ITCMon to decode the received messages
4. An IGW connection to make decoded packet data available to another application or service

ITCMon handles the radio-message decoding. ItcVue begins after that decoding process.

## What Is ItcVue?

ItcVue is a receive-only application that converts decoded ITC wayside data into live graphical views of railroad territory.

The application uses decoded Wayside Status Messages to display information such as:

- Signal indications
- Switch positions
- Wayside equipment states
- Changes in those states over time

The resulting display is similar in concept to a dispatcher-style railroad diagram, but ItcVue does not connect to or control railroad infrastructure.

ItcVue is intended to support both local and hosted use. A user may run the software with a local receiver or connect to receiver data made available through hosted services.

## High-Level Data Flow

```text
Wayside radio transmissions
          ↓
Software-defined radio
          ↓
Radio demodulation software
          ↓
ITCMon
Decodes supported ITC messages
          ↓
IGW receiver connection
          ↓
ItcVue hosted services
Authenticate, process, route, and store packet observations
          ↓
Azure SignalR Service
          ↓
ItcVue clients
          ↓
Live graphical signal and switch display
```

## Major ItcVue Components

### Receiver Sites

Receiver sites listen for ITC transmissions and run the software required to decode them. Multiple receiver sites may receive the same underlying radio packet.

Each valid reception is treated as a packet observation associated with the receiver that submitted it.

### Hosted Packet Ingestion

Hosted services authenticate receiver connections and accept decoded packet observations from IGW receivers.

Accepted observations are normalized and published to an Azure Service Bus topic for downstream processing.

### Observation Processing

Separate Service Bus subscriptions can process each observation for different purposes, including:

- Database storage
- Reporting
- Live client publication

Every valid packet observation remains available to the database and reporting paths.

The live-publication path applies short-window duplicate suppression so that the same underlying radio packet is not repeatedly sent to connected clients.

### Azure SignalR Service

The live-publication processor sends client-facing messages to Azure SignalR Service.

Clients can subscribe to groups associated with individual Wayside Interface Unit addresses. This allows an ItcVue client to receive only the updates needed for the territory or layout it is currently displaying.

### ItcVue Client

The ItcVue client is built with Vue and presents the live railroad display.

The client includes or is planned to include:

- A graphical territory viewer
- A layout editor
- Reusable layout components
- Signal and switch visualization
- Desktop and browser-based access
- Shared territory layouts

## Simplified Hosted Architecture

```text
IGW receiver sites
        ↓
Authenticated packet ingestion
        ↓
Azure Service Bus topic
   ┌──────────────┼────────────────┐
   ↓              ↓                ↓
Database       Reporting       Live publish
subscription   subscription    subscription
                                   ↓
                              Azure Function
                                   ↓
                     Short-window duplicate suppression
                                   ↓
                         Azure SignalR Service
                                   ↓
                          Connected ItcVue clients
```

## Project Scope

ItcVue is an independent, receive-only visualization and data-processing project.

It does not:

- Transmit over the ITC radio network
- Send commands to railroad equipment
- Control signals or switches
- Participate in railroad operations
- Replace or modify any part of the operational PTC system

Its role is to receive already-transmitted radio data, process supported decoded messages, and present that information through a graphical interface.
