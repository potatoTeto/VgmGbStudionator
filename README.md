# furGBVGMHeaderRemover
## Strip the GB Header from a v1.72 Furnace .VGM Export for GBDK

fur2uge is a conversion tool that strips the GB Header from v1.72 .vgm exports exported from a .fur project file. The tool is designed to allow [Furnace](https://github.com/tildearrow/furnace) users to convert their SFX project files to properly-formatted .vgm files, so that they can be exported for homebrew use (including [GB Studio](https://www.gbstudio.dev/)).

# Download
https://github.com/potatoTeto/furGBVGMHeaderRemover/releases

## Usage
### Casual Usage
Place all of your prepared v1.72 .vgm files in the ``/input/`` folder, preferably exported using [Furnace](https://github.com/tildearrow/furnace) with a Base Tempo of 640 (256Hz). Ensure that this ``/input/`` folder is located at the same location that the program is. Double-click on ``convert.bat`` to get your converted .vgm files in the /output/ folder.

### Terminal Usage
``furGBVGMHeaderRemover <input>.vgm``
(Outputs to a relative /output/ folder)

## Caveats
- Designed specifically with v1.72 .vgm file exports in mind, using [Furnace](https://github.com/tildearrow/furnace). When exporting, ensure that "Loop song" is left unchecked.
- For more accurate results, avoid changing the Base Tempo from 640 (256Hz). Feel free to modify Speed, though, including via Fxx in the middle of the SFX.
- Several FX columns are allowed, at least as far as GB Studio goes.
- Try to keep your pattern count to 20 rows or lower. Unexpected bugs might occur for longer SFX (untested)
- If your SFX is designed for GBDK (including GB Studio), put an FF99 at the row you want the sound to end on
- Avoid using Stereo Pan anywhere in your SFX: They tend to be buggy!

# Special Thanks
- [Beatscribe](https://github.com/Beatscribe)
- [coffeevalenbat](https://github.com/coffeevalenbat)

## License

MIT
