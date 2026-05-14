# Mental Sphere — Alpha 0.1

Release status: **Alpha**

This version is an early demonstrable prototype of the diploma project "Mental Sphere" / "Ментальна куля".

## Purpose

The build demonstrates the core idea of a distributed VR relaxation system:

- Unity VR client;
- hub with interactive spheres;
- server-driven generation profiles;
- procedural runtime terrain and environment spawning;
- basic transition flow between hub and generated locations;
- return-to-hub interaction component;
- preliminary environment restoration for the hub.

## Current state

Implemented or partially implemented:

- XR/URP Unity project structure;
- GameManager-driven generation flow;
- sphere activation routing into GameManager;
- runtime terrain generation;
- object spawning by categories;
- water controller for lake biome;
- biome atmosphere application;
- return-to-hub sphere script;
- hub environment restorer script;
- preliminary performance/telemetry-related components.

## Known limitations

This is not a beta version yet. The prototype is still expected to contain bugs and incomplete configuration.

Known areas requiring verification or further work:

- server must remain the source of full biome profiles;
- local detailed fallback profiles should not be used for final distributed-system demonstration;
- generated object density requires tuning on real Meta Quest 2 hardware;
- flower-field density may need prefab-patch optimization instead of spawning thousands of separate objects;
- visual effects such as god rays and glowing particles should be tested carefully for VR performance;
- build size and FPS must be verified on target hardware;
- CI/CD pipeline is not considered finalized in this release.

## Recommended demo scenario

For a teacher/supervisor demo, use this version as an **Alpha prototype**:

1. Launch the Unity project.
2. Start the FastAPI server.
3. Enter the VR hub.
4. Activate different spheres.
5. Show that generation profiles differ by sphere color.
6. Demonstrate generated lake/forest/flower-field logic.
7. Demonstrate return to hub.
8. Explain that further work focuses on visual polishing, server profile tuning, optimization, and CI/CD.

## Version name

**Alpha 0.1 — Core VR Generation Prototype**
