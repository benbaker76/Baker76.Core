# Pngcs

A small library to read/write huge PNG files in C#

## Overview

Pngcs is a lightweight C# library for reading and writing PNG images in a **progressive**, line-oriented fashion—perfect for very large images that you don’t want to load entirely into memory.

- **Simple API** for sequential reading/writing
- **Supports all PNG color models & bit-depths**  
  RGB8/RGB16, RGBA8/RGBA16, G8/4/2/1, GA8/4/2/1, PAL8/4/2/1  
- **All filter & compression settings** (no interlacing)
- **Chunk (metadata) support**
- **Ideal for “streaming”** huge images

This library is a C# port of the [PngJ](http://code.google.com/p/pngj/) Java library—its API, documentation and samples apply here as well.

## Getting Started

1. Clone or download the repository.
2. Browse the `docs/` folder for full API documentation.
3. Check out the included sample projects to see Pngcs in action.

## License

This project is released under the **Apache 2.0 License**.  
See [LICENSE](LICENSE) for full terms.

## History & Changes

All notable changes, bug-fixes and version history are detailed in [changes.txt](changes.txt).

## Author

**Hernan J González**  
- Email: [hgonzalez@gmail.com](mailto:hgonzalez@gmail.com)  
- StackOverflow: [leonbloy](http://stackoverflow.com/users/277304/leonbloy)  
