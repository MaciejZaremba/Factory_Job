Factory Job

A procedurally generated dungeon crawler featuring card-based combat.

Gameplay Features

&#x20;   Procedural Generation: Uses a custom Wave Function Collapse (WFC) inspired algorithm to generate layered factory floors.

    Card Combat: Strategic turn-based battle system using a deck of Attack, Defense, and Utility cards.

    Event System: Randomly encountered narrative events that affect player stats and progress.


Technical Architecture

This project is built using Godot 4.x and C# (.NET 8.0).

Key Systems

&#x20;   AlgoGrid: The core generation engine that handles tile constraints and connectivity.

    CombatManager: A state-machine-driven system managing player/enemy turns and card resolution.

    TileRegistry: A dynamic resource loader that scans res://data/tiles/ to register game pieces at runtime.


Design Patterns Used

&#x20;   Singleton (Autoloads): Used for global managers like EventManager, CombatManager, and GameState.

    Resource-Based Data: All tiles, enemies, and events are stored as .tres files for easy balancing without touching code.


Getting Started
Prerequisites

&#x20;   Godot Engine 4.x (v4.2+ recommended)

    .NET SDK 8.0+


Installation

&#x20;   Clone the repository:
    Bash

    git clone https://github.com/MaciejZaremba/Factory\_Job.git

    Open project.godot in the Godot Editor.

    Build the solution (Build button in the top right or Shift + Ctrl + B).

    You can also try the .exe file but I don't know if it will work.


Project Structure

&#x20;   /scenes: Visual nodes and UI layouts.

    /scripts: C# logic divided by functionality (Generator, Game, UI).

    /data: .tres Resource files for Tiles, Events, and Cards.

AI usage

&#x20;   A mix of Claude.ai and Gemini were used to help with debugging, as a tool for learining the Godot Engine, as well as to help write this document.


Controls
&#x20;   Player movement: WASD
&#x20;   Camera movement: mouse movement
&#x20;   Sprint: Hold Shift
&#x20;   Open/Close Inventory: Esc
&#x20;   Equip/Unequip items: Drag and drop them to appropriate slots
&#x20;   Combat: Drag and drop cards onto the middle part of the screen to play them, press End Phase button to end your turn.
