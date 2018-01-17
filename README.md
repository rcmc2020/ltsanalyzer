# Cycling Level of Traffic Stress OSM Analyzer

This is a console application that takes an OSM file and performs an analysis on the streets based on the information stored in their tags. The format is as follows:

 `ltsanalyzer -f filename -d outputpath [-o outputtype][-p prefix]`
 
 where:
 
 * filename   is the path to an OSM file to be processed.
 * outputpath is the directory where the output files will be created.
 * outputtype is the type of the file to be generated.  It must either be 'osm' or 'geojson'.
 * prefix     is the prefix to be appended to the start of the filename. 
  
See the usage output for an up-to-date list of options.

 ## Example ##
 
 `ltsanalyzer -f c:\data\myosmfile.osm -o geojson -p lts_`
 
 will analyze the specified OSM file and produce 4 output files in geojson format. The files will be named lts_1.json, lts_2.json, lts_3.json and lts_4.json and each will contain the streets for the corresponding LTS level (1-4).
 
 If you are using [stressmap](https://github.com/rcmc2020/stressmap) to display the files, you should generate the files as geojson with the default value for the prefix "level_". This data should be placed in the app/data directory. For more information, see the documentation for stressmap.
