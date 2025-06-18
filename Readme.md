# Unity SplatTour
This project is an implementation of a Unity WebViewer for 3D Gaussian Splatting, specifically made for the use in Virtual Tours on WebGPU. The project was initially created for benchmarking the performance of 3D Gaussian Splatting in Unity on Web-based platform.

:warning: This project is customized to work only on WebGPU. Right now it is only tested and verified on Windows based systems.

:warning: Because of limitations in WebGPU rendering, the Radix Sorting of 3D gaussian splats was moved to the CPU. This may have a significant impact on larger scenes (more than 1M splats).

## Radiance Field Rendering in Virtual Tours
This project is part of the thesis "Radiance Field Rendering in Virtual Tours" for obtaining the Master in Game Technology at Breda University of Applied Sciences. For more information, the full thesis project can be found on the [Github Project Page](https://friesboury.github.io/splattour).

## Usage

*coming soon*

## License and External Code Used
This Project was written under [MIT License](/LICENSE.md) and was based on the project [UnityGaussianSplatting](https://github.com/aras-p/UnityGaussianSplatting) Aras-P, MIT License (c) 2023 Aras Pranckevičius.

However, keep in mind that the license of the original paper implementation says that the official training software for the Gaussian Splats is for educational / academic / non-commercial purpose; commercial usage requires getting license from INRIA. That is: even if this viewer / integration into Unity is just "MIT license", you need to separately consider how did you get your Gaussian Splat PLY files.