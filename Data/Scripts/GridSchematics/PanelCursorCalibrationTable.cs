namespace GridSchematics
{
    public partial class GridSchematicsSession
    {
        // Generated from local panel calibration/depth catalogs. Local player calibration entries are appended after this catalog at runtime and override matching built-in entries.
        const string BuiltInPanelCursorCalibrationCatalog = @"----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_Cockpit/OpenCockpitLarge
BlockDisplayName=Unknown
DebugBlockEntityId=139605899333074699
ModelEntityName=
DetectedSurfaceCount=5

ScreenCalibration[-1]:
ScreenIndex=-1
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.0010283686860930175, -0.449957191827707, -0.72133009042590857)
NormalLocal=new Vector3D(3.7745123276922143E-09, 0.91850471861197658, 0.39541006931789613)
AxisALocal=new Vector3D(1.0000000071513657, -3.76173157033699E-09, 1.7783349043343222E-08)
AxisBLocal=new Vector3D(-5.827658910995126E-10, 0.39541008960459323, -0.91850467820779369)
RawMinA=-0.32392341823466586
RawMaxA=0.32036370231980377
RawMinB=-0.19461911921564426
RawMaxB=0.1610243064950927
OffsetA=0.0030000000000000005
OffsetB=-0.0015
ScaleA=0.98159904089434813
ScaleB=0.99049133563902081
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.31499566775585536
FinalMaxA=0.31743595184099321
FinalMinB=-0.19442827223200815
FinalMaxB=0.15783345951145655

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_Cockpit/LargeBlockLabDeskSeat
BlockDisplayName=Unknown
DebugBlockEntityId=87917391517317520
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[-1]:
ScreenIndex=-1
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.0022147564159240574, 0.16481747003854252, -0.9394576393533498)
NormalLocal=new Vector3D(1.9824710262295042E-08, -3.1583634318987919E-09, 0.99999998358121645)
AxisALocal=new Vector3D(1.9043216344105662E-09, 1.0000000287194293, 5.5862016770137635E-09)
AxisBLocal=new Vector3D(-1.0000000071513657, 1.3452900182731042E-09, -4.0822178970501E-11)
RawMinA=-0.24275817688720061
RawMaxA=0.12811536955689673
RawMinB=-0.30467391924925058
RawMaxB=0.29538663200032317
OffsetA=0.016499999999999997
OffsetB=-0.010499999999999999
ScaleA=1.059225668557026
ScaleB=0.9967894168755993
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.23724079375633406
FinalMaxA=0.15559798642603018
FinalMinB=-0.31421064710952035
FinalMaxB=0.28392335986059297

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_Cockpit/OpenCockpitLarge
BlockDisplayName=Unknown
DebugBlockEntityId=139605899333074699
ModelEntityName=
DetectedSurfaceCount=5

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=ManualRectangle
SeedLocal=new Vector3D(-0.727429100923473, -0.42136071517597884, -0.71459562471136451)
NormalLocal=new Vector3D(0.5134206848890559, 0.68760341403822489, 0.513420662030117)
AxisALocal=new Vector3D(0.30717793760064138, 0.4113909359527011, -0.85813704657056666)
AxisBLocal=new Vector3D(-0.80127456546541509, 0.59829683266065525, -2.7755575615628914E-17)
RawMinA=-0.16933593749999998
RawMaxA=0.12556640624999998
RawMinB=-0.11642578124999999
RawMaxB=0.15890624999999997
OffsetA=0
OffsetB=0
ScaleA=1
ScaleB=1
CursorDepthOffset=-0.0007499999999999998
ManualWidthA=0.29490234374999996
ManualHeightB=0.27533203124999994
FinalMinA=-0.16933593749999998
FinalMaxA=0.12556640624999998
FinalMinB=-0.11642578124999997
FinalMaxB=0.15890624999999997

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_Cockpit/OpenCockpitLarge
BlockDisplayName=Unknown
DebugBlockEntityId=139605899333074699
ModelEntityName=
DetectedSurfaceCount=5

ScreenCalibration[1]:
ScreenIndex=1
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.76964411902008578, -0.4171915874176193, -0.70912233367562294)
NormalLocal=new Vector3D(0.51342068426375376, 0.68760341057767338, 0.51342066703253386)
AxisALocal=new Vector3D(0.70700029685180121, 0.00015898865077411742, -0.70721321086861044)
AxisBLocal=new Vector3D(-0.486363850956804, 0.7260864833449554, -0.48605417350150348)
RawMinA=-0.22130859374999995
RawMaxA=0.17712890625
RawMinB=-0.15357421875
RawMaxB=0.1217578125
OffsetA=-0.006
OffsetB=0.011999999999999999
ScaleA=0.82937299953008292
ScaleB=0.82937299953008292
CursorDepthOffset=-0.00074999999999999806
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.19331649600013368
FinalMaxA=0.13713680850013371
FinalMinB=-0.1180846794372615
FinalMaxB=0.11026827318726151

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_MedicalRoom/LargeMedicalRoomReskin
BlockDisplayName=Unknown
DebugBlockEntityId=86062945346408856
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(1.2310487451613881, 0.34024044900434092, -1.2045351886190474)
NormalLocal=new Vector3D(-0.984737420315956, 0.17404662834669365, -1.5735543507089389E-08)
AxisALocal=new Vector3D(-2.3907644197684874E-09, 1.0451099285904014E-08, -0.99999998318201389)
AxisBLocal=new Vector3D(-0.17404662411614355, -0.98473744106662819, -1.098767364593467E-08)
RawMinA=-0.19769930542120495
RawMaxA=0.20366787616822529
RawMinB=-0.13530781253034252
RawMaxB=0.10442337790940512
OffsetA=0.0015000000000000005
OffsetB=0.013499999999999998
ScaleA=0.94631372663113844
ScaleB=0.86742695660813174
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.18542535130515511
FinalMaxA=0.19439392205217546
FinalMinB=-0.10591686577406607
FinalMaxB=0.10203243115312867

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_MyProgrammableBlock/SmallProgrammableBlock
BlockDisplayName=Unknown
DebugBlockEntityId=106080450374102103
ModelEntityName=
DetectedSurfaceCount=2

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.023384877247735861, 0.024759200197877369, 0.029457653436111293)
NormalLocal=new Vector3D(-7.10909942025495E-10, 0.17359569898304214, 0.98481700094869085)
AxisALocal=new Vector3D(0.99999996893671306, 2.9687526525642993E-10, -7.05267319545122E-09)
AxisBLocal=new Vector3D(6.9388939039072284E-18, 0.98481697242283517, -0.17359571679313635)
RawMinA=-0.2038670133126543
RawMaxA=0.15714273979933394
RawMinB=-0.13779396484458289
RawMaxB=0.19618394470399297
OffsetA=0
OffsetB=-0.0075
ScaleA=1.0442025053432542
ScaleB=1.0442025053432542
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.21184578108310406
FinalMaxA=0.16512150756978369
FinalMinB=-0.15267529501025781
FinalMaxB=0.19606527486966788

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_MyProgrammableBlock/LargeProgrammableBlock
BlockDisplayName=Unknown
DebugBlockEntityId=73106115733861996
ModelEntityName=
DetectedSurfaceCount=2

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.14181254807044752, 0.2213210015615914, 0.99785037571564317)
NormalLocal=new Vector3D(2.0265622041604203E-08, 0.0082551846592241636, 0.99996587561955119)
AxisALocal=new Vector3D(0.99999997332920276, -2.7372713470863452E-11, -2.8842321309063834E-09)
AxisBLocal=new Vector3D(0, 0.99996595306526437, -0.00825518049171869)
RawMinA=-0.56905862664502682
RawMaxA=0.28246674096864094
RawMinB=-0.24564416302753572
RawMaxB=0.24594082668998529
OffsetA=0.0015
OffsetB=0
ScaleA=1.0174207520537144
ScaleB=1.0718533609055609
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.57497573279334968
FinalMaxA=0.29138384711696386
FinalMinB=-0.26330517986850044
FinalMaxB=0.26360184353095006

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_MedicalRoom/LargeRefillStation
BlockDisplayName=Unknown
DebugBlockEntityId=80420915891266790
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.228247821360128, 0.34643548727035522, 0.86469921842217445)
NormalLocal=new Vector3D(-1.7124106466148703E-08, 3.928980528509346E-09, -0.99999995020426191)
AxisALocal=new Vector3D(-1.3992366813819768E-10, 1.00000002768174, 4.0094539754259584E-10)
AxisBLocal=new Vector3D(0.99999997332920276, -1.7347234759768071E-18, 2.565928303166487E-10)
RawMinA=-0.25335688100127296
RawMaxA=0.19429889340561807
RawMinB=-0.345496347257106
RawMaxB=0.31150998255049345
OffsetA=0.006
OffsetB=0.013499999999999998
ScaleA=1.0086727675781251
ScaleB=1.0264803574218753
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.24929808824449123
FinalMaxA=0.20224010064883635
FinalMinB=-0.34069522847797584
FinalMaxB=0.33370886377136333

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_MedicalRoom/InsetRefillStation
BlockDisplayName=Unknown
DebugBlockEntityId=122520076203746768
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.23802950908429921, 0.34657275679637678, -1.0274140625551809)
NormalLocal=new Vector3D(1.7380698880131717E-08, 1.3992374273130714E-10, -0.99999997332920276)
AxisALocal=new Vector3D(0.99999995020426191, 3.52803509106811E-09, 0)
AxisBLocal=new Vector3D(-3.903127820947816E-18, -1.0000000276817398, -1.7347234759768071E-18)
RawMinA=-0.35527802793181956
RawMaxA=0.32740016232811608
RawMinB=-0.20664990522713977
RawMaxB=0.25349415047558943
OffsetA=0
OffsetB=0
ScaleA=0.99071887395859148
ScaleB=0.99071887395859148
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.352110016767058
FinalMaxA=0.32423215116335458
FinalMinB=-0.20451457773804882
FinalMaxB=0.25135882298649848

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_Cockpit/LargeBlockCockpit
BlockDisplayName=Unknown
DebugBlockEntityId=119129214223463024
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.062891457404475659, 0.34429125001770444, -0.34719919692724943)
NormalLocal=new Vector3D(1.31982880180459E-08, 0.25868054899597831, 0.9659628800570802)
AxisALocal=new Vector3D(0.99999997334961366, -5.35224409237145E-09, 5.3508305564164971E-09)
AxisBLocal=new Vector3D(2.6808208986039972E-09, 0.96596295411417521, -0.25868052592768209)
RawMinA=-0.745997589160851
RawMaxA=0.60626018706922846
RawMinB=-0.45358613157601391
RawMaxB=0.28956467388803309
OffsetA=0.0075
OffsetB=0.013499999999999998
ScaleA=1.0063585424828729
ScaleB=1.0252236407708448
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.74279678341982813
FinalMaxA=0.61805938132820559
FinalMinB=-0.44945861605380844
FinalMaxB=0.31243715836582764

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_Cockpit/CockpitOpen
BlockDisplayName=Unknown
DebugBlockEntityId=130146786032067229
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.016034910659072921, -0.070005497662350535, -0.2633814497385174)
NormalLocal=new Vector3D(7.98585961248044E-09, 0.49996421334065871, 0.866046029082587)
AxisALocal=new Vector3D(0.999999973329203, -8.6722641394476607E-10, 9.2718173694272821E-09)
AxisBLocal=new Vector3D(-2.385689507056199E-09, 0.86604609629086537, -0.49996417463535253)
RawMinA=-0.72111175608342171
RawMaxA=0.68904445887215471
RawMinB=-0.19627801135943257
RawMaxB=0.100679293857254
OffsetA=0
OffsetB=0
ScaleA=0.99954071291827085
ScaleB=0.97353168159747572
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.72078792281704707
FinalMaxA=0.68872062560578007
FinalMinB=-0.19234803110621715
FinalMaxB=0.096749313604038573

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TextPanel/ATL_SingleLCDPanel_LG
BlockDisplayName=Unknown
DebugBlockEntityId=141052893319581911
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.1355652421771083, 0.14778203662717715, 1.1727815382182598)
NormalLocal=new Vector3D(-1.71739635290713E-08, -2.0381223197855902E-09, -0.99999994661892322)
AxisALocal=new Vector3D(5.5115252813042748E-10, 1.0000000277971897, -5.1852561762551208E-09)
AxisBLocal=new Vector3D(0.99999996967210647, -1.3790652456796559E-09, 2.974135937527933E-10)
RawMinA=-1.3333286278406153
RawMaxA=1.0377651897866513
RawMinB=-1.0499803254834168
RawMaxB=1.3211133526465437
OffsetA=0
OffsetB=0
ScaleA=0.99946418558243788
ScaleB=0.99946418558243788
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-1.3326933947141768
FinalMaxA=1.0371299566602126
FinalMinB=-1.0493450923943506
FinalMaxB=1.3204781195574773

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TextPanel/ATL_HalfSlopeArmorBlockTipLCD_LG
BlockDisplayName=Unknown
DebugBlockEntityId=74626165756151499
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.017718669027090073, -0.10411548858974129, 0.48540538467932492)
NormalLocal=new Vector3D(1.3033615742230609E-08, 0.44733208175015687, -0.89436791752952549)
AxisALocal=new Vector3D(0.999999946618923, -2.6832204865108455E-09, -5.81452877512767E-09)
AxisBLocal=new Vector3D(1.3877787807814457E-17, -0.89436796914438466, -0.44733205500848461)
RawMinA=-1.203265486608708
RawMaxA=1.167828148730818
RawMinB=-1.3412803326879441
RawMaxB=1.0300517264939635
OffsetA=0
OffsetB=0
ScaleA=1
ScaleB=1
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-1.203265486608708
FinalMaxA=1.167828148730818
FinalMinB=-1.3412803326879441
FinalMaxB=1.0300517264939635

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TextPanel/ATL_HalfSlopeArmorBlockBaseLCD_LG
BlockDisplayName=Unknown
DebugBlockEntityId=123123026098260637
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.24098145961761475, 0.0011583696468733251, -0.71207051794044673)
NormalLocal=new Vector3D(6.0508532351288835E-09, 0.44733720478143818, -0.89436535469682088)
AxisALocal=new Vector3D(0.99999994653021151, 5.5504579231369089E-10, -1.2010310995735907E-08)
AxisBLocal=new Vector3D(1.3877787807814457E-17, -0.89436540665997932, -0.44733717892506542)
RawMinA=-1.4265282641398436
RawMaxA=0.94456537059555823
RawMinB=-1.2236310767392018
RawMaxB=1.1478238447507916
OffsetA=0
OffsetB=0
ScaleA=1
ScaleB=1
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-1.4265282641398436
FinalMaxA=0.94456537059555812
FinalMinB=-1.2236310767392018
FinalMaxB=1.1478238447507918

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TextPanel/ATL_SlopedArmorBlockLCD_LG
BlockDisplayName=Unknown
DebugBlockEntityId=121172305377447296
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.028039664495736361, -0.18556482493295334, -0.29625271848635748)
NormalLocal=new Vector3D(2.0224346641839475E-09, 0.70710683926250906, -0.70710672116686546)
AxisALocal=new Vector3D(0.99999994653021163, 1.8317402403905669E-09, -1.5991907520440662E-08)
AxisBLocal=new Vector3D(2.7755575615628914E-17, -0.70710676229260061, -0.70710679830648726)
RawMinA=-1.1575071644197716
RawMaxA=1.2135864825955736
RawMinB=-1.5263432700954587
RawMaxB=0.84495036623099085
OffsetA=0
OffsetB=0
ScaleA=1
ScaleB=1
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-1.1575071644197719
FinalMaxA=1.2135864825955736
FinalMinB=-1.5263432700954587
FinalMaxB=0.84495036623099073

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TextPanel/ATL_ArmorBlockLCD_LG
BlockDisplayName=Unknown
DebugBlockEntityId=140894231538979475
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.14106417610310018, 0.17668184000649489, -1.202221109502716)
NormalLocal=new Vector3D(2.9988317662432706E-09, -2.4578867413582728E-10, -0.99999996967210636)
AxisALocal=new Vector3D(5.1238846642161218E-09, 1.0000000277575622, -6.1401293424689918E-10)
AxisBLocal=new Vector3D(0.99999994657456748, -2.0342231644814024E-09, -1.4458938019101808E-08)
RawMinA=-1.3622285846531887
RawMaxA=1.00886523879156
RawMinB=-1.326620239613054
RawMaxB=1.0444733837615423
OffsetA=0
OffsetB=0
ScaleA=1
ScaleB=1
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-1.362228584653189
FinalMaxA=1.00886523879156
FinalMinB=-1.326620239613054
FinalMaxB=1.0444733837615425

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_LCDPanelsBlock/LargeBlockLabDeskMicroscope
BlockDisplayName=Unknown
DebugBlockEntityId=118317265592172282
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.2386619480967056, 0.12665706768166274, -0.9394279564730823)
NormalLocal=new Vector3D(1.7143214903203585E-08, 2.8320123980282719E-10, 0.99999994653021151)
AxisALocal=new Vector3D(1.6733133767488084E-09, 1.00000002777306, -4.0094538886897846E-10)
AxisBLocal=new Vector3D(-0.99999996965849924, 9.0248938530712586E-10, -2.5659188662707777E-10)
RawMinA=-0.20459899893961961
RawMaxA=0.19463196965137783
RawMinB=-0.33848578200739365
RawMaxB=0.28184500974404664
OffsetA=0.0015
OffsetB=0
ScaleA=0.9904913356390207
ScaleB=0.97360622332394908
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.20120092229819939
FinalMaxA=0.19423389300995761
FinalMinB=-0.33029934581601095
FinalMaxB=0.27365857355266388

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TextPanel/LargeDiagonalLCDPanel
BlockDisplayName=Unknown
DebugBlockEntityId=126948447243316580
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.30570986459497362, 0.25443279740284197, -0.30853858985938132)
NormalLocal=new Vector3D(0.70710675243949817, 1.76230359385382E-09, -0.70710673832849491)
AxisALocal=new Vector3D(-2.034446130658174E-09, 1.0000000276541772, -2.1878364901001257E-09)
AxisBLocal=new Vector3D(0.70710676703866759, -1.7347234759768071E-18, 0.70710674836417131)
RawMinA=-1.4487687815647519
RawMaxA=0.939903170045883
RawMinB=-1.3334276611861238
RawMaxB=2.2021061273668208
OffsetA=0
OffsetB=0
ScaleA=0.99102233969675657
ScaleB=0.99102233969675657
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-1.438046438886029
FinalMaxA=0.92918082736716
FinalMinB=-1.3175572505139899
FinalMaxB=2.1862357166946871

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TextPanel/LargeFullBlockLCDPanel
BlockDisplayName=Unknown
DebugBlockEntityId=95917589042815029
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(1.2510000001639128, 0.28187137845088728, 0.037996597500750795)
NormalLocal=new Vector3D(0.99999994648585577, -3.8349190181925785E-09, -1.7162874565990194E-08)
AxisALocal=new Vector3D(2.5659191438265339E-10, -7.88309374374907E-10, -0.99999996966870464)
AxisBLocal=new Vector3D(4.009455016260044E-10, 1.0000000276541772, 3.4694469519536142E-18)
RawMinA=-1.2120033663922727
RawMaxA=1.2879965596453764
RawMinB=-1.4762073567659308
RawMaxB=0.91246458428899679
OffsetA=0
OffsetB=0
ScaleA=0.99109822065427544
ScaleB=0.99109822065427544
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-1.2008761425393153
FinalMaxA=1.276869335792419
FinalMinB=-1.4655756414916337
FinalMaxB=0.90183286901469972

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TextPanel/HoloLCDLarge
BlockDisplayName=Unknown
DebugBlockEntityId=81538666391390267
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.04796784371137619, 0.81411272287368774, -0.82324230670928955)
NormalLocal=new Vector3D(2.8259141381103969E-10, -5.4411601526499487E-11, 1.0000000277213672)
AxisALocal=new Vector3D(0.9999999696516958, -1.7364021304144828E-08, 0)
AxisBLocal=new Vector3D(-2.7755575615628914E-17, 0.99999994648585555, 5.6378512969246231E-18)
RawMinA=-1.2020321273920904
RawMaxA=1.2979678401535857
RawMinB=-2.0641126124966775
RawMaxB=0.43588725376512655
OffsetA=0
OffsetB=0
ScaleA=1
ScaleB=1
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-1.2020321273920906
FinalMaxA=1.2979678401535857
FinalMinB=-2.0641126124966775
FinalMaxB=0.43588725376512649

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_Cockpit/LargeBlockCaptainDesk
BlockDisplayName=Unknown
DebugBlockEntityId=89586987437799925
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.75315803289413452, -0.095346450805664063, -0.78667688361019827)
NormalLocal=new Vector3D(-0.70667200139217312, 4.1832945996134185E-09, 0.70754124698220511)
AxisALocal=new Vector3D(-6.501731921945697E-10, 1.0000000277730599, -2.189029346677529E-09)
AxisBLocal=new Vector3D(-0.707541218365566, 8.6736173798840355E-19, -0.7066720053432608)
RawMinA=-0.1680441796583407
RawMaxA=0.13760670839383096
RawMinB=-0.2192032893058313
RawMaxB=0.33883215099245639
OffsetA=-4.3368086899420177E-19
OffsetB=-0.013499999999999998
ScaleA=1.1487816669537081
ScaleB=0.9908705952101815
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.19078180397348235
FinalMaxA=0.16034433270897261
FinalMinB=-0.23015602359505744
FinalMaxB=0.32278488528168253

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_Cockpit/LargeBlockLabDeskSeat
BlockDisplayName=Unknown
DebugBlockEntityId=106650770110608709
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.0022507072426378731, 0.15652823165874, -0.9392610755749049)
NormalLocal=new Vector3D(1.7153287901194858E-08, 5.1309023653065E-10, 0.99999994670763481)
AxisALocal=new Vector3D(-1.0743558629539018E-09, 1.0000000279315704, -4.0094493133566167E-10)
AxisBLocal=new Vector3D(-0.999999969644892, -1.8218662134472652E-09, -2.56591942138229E-10)
RawMinA=-0.23447676661190464
RawMaxA=0.13639677926081761
RawMinB=-0.3001692545686856
RawMaxB=0.29989127373456242
OffsetA=0.016499999999999997
OffsetB=-0.010499999999999999
ScaleA=1.0706230638083714
ScaleB=1.007823601103
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.23107287965940776
FinalMaxA=0.16599289230832071
FinalMinB=-0.31301657167423563
FinalMaxB=0.2917385908401125

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TextPanel/TransparentScreenBack
BlockDisplayName=Unknown
DebugBlockEntityId=75041240720048676
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.41734860884025693, -0.71665839504566975, -1.1903790626383852)
NormalLocal=new Vector3D(-1.769494173409214E-08, 1.5495434848439293E-09, 0.99999996230688726)
AxisALocal=new Vector3D(-2.0154560915162933E-09, 1.0000000277334324, -1.4069322980952981E-09)
AxisBLocal=new Vector3D(-0.99999993913775531, -1.8045950398067057E-09, -3.0379432303107023E-10)
RawMinA=-0.36732531619889952
RawMaxA=1.8006434938393809
RawMinB=-0.66664655913193971
RawMaxB=1.5013220588244536
OffsetA=0
OffsetB=0
ScaleA=0.9999234375
ScaleB=0.9999234375
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.36724232364289033
FinalMaxA=1.8005605012833716
FinalMinB=-0.66656356658328364
FinalMaxB=1.5012390662757975

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TextPanel/LargeLCDPanel
BlockDisplayName=Unknown
DebugBlockEntityId=128852065637747167
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.039815053110942245, 0.27201452845474705, -1.0214608183305245)
NormalLocal=new Vector3D(-1.7719637951385536E-08, -2.4960682826202785E-10, 0.9999999623204946)
AxisALocal=new Vector3D(0.999999939182111, -3.4182674403700208E-09, 2.7052748929889958E-10)
AxisBLocal=new Vector3D(4.3368086899420177E-18, 1.0000000277179348, -1.0381846424212249E-11)
RawMinA=-1.274189979103602
RawMaxA=1.1945598793178229
RawMinB=-1.5063895702430559
RawMaxB=0.96236049818779956
OffsetA=0
OffsetB=0
ScaleA=1
ScaleB=1
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-1.274189979103602
FinalMaxA=1.1945598793178229
FinalMinB=-1.5063895702430559
FinalMaxB=0.96236049818779967

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TextPanel/LargeLCDPanelWide
BlockDisplayName=Unknown
DebugBlockEntityId=111644530274517355
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.898979484802112, 0.4155620634846855, -1.0224608182616066)
NormalLocal=new Vector3D(-1.7686371062142214E-08, 1.7570880514461429E-10, 0.99999996231028909)
AxisALocal=new Vector3D(-3.8705495405966239E-11, 1.0000000277851251, -1.0381934895109524E-11)
AxisBLocal=new Vector3D(-0.999999939182111, 0, -2.7052746154332397E-10)
RawMinA=-1.6499371092182424
RawMaxA=0.81881295938463639
RawMinB=-3.3872605290249291
RawMaxB=1.5893016683117391
OffsetA=0
OffsetB=0
ScaleA=0.9999234375
ScaleB=0.9999234375
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-1.6498426023796788
FinalMaxA=0.81871845254607289
FinalMinB=-3.3870700200033124
FinalMaxB=1.5891111592901224

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TextPanel/TSOCorRndLCD
BlockDisplayName=Unknown
DebugBlockEntityId=73819407932215242
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.93464054010109976, 0.22082886096904986, -0.020960978697985411)
NormalLocal=new Vector3D(-0.99999995861237179, -3.0596189956599851E-09, -1.7751335873450458E-08)
AxisALocal=new Vector3D(3.1258282073842736E-10, 0.00872654244174579, -0.99996185873146148)
AxisBLocal=new Vector3D(2.8063174128262203E-09, -0.99996195097584029, -0.0087265423855034038)
RawMinA=-0.70249790769270037
RawMaxA=0.64752544137656243
RawMinB=-0.35137361748223711
RawMaxB=0.805571727538954
OffsetA=0.006
OffsetB=-0.039000000000000014
ScaleA=0.98973325193036232
ScaleB=0.989809034184542
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.689567732886189
FinalMaxA=0.64659526657005106
FinalMinB=-0.38447842225150497
FinalMaxB=0.76067653230822185

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TextPanel/TSOCorRndTLCD
BlockDisplayName=Unknown
DebugBlockEntityId=96257389093359993
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.057611122028902173, 0.28025103235268034, -0.91364074344164692)
NormalLocal=new Vector3D(-1.768480212271939E-08, -2.6651813395117685E-10, 0.99999995861237179)
AxisALocal=new Vector3D(0.99996185879645882, -0.0087265349937597509, 2.7042568184754145E-10)
AxisBLocal=new Vector3D(0.0087265349375181059, 0.99996195104083807, 1.2742487620620579E-11)
RawMinA=-0.61039731560245691
RawMaxA=0.73962603377258929
RawMinB=-0.86469664609405894
RawMaxB=0.29224869908971823
OffsetA=-0.0045000000000000005
OffsetB=0.036000000000000011
ScaleA=0.98182453645217915
ScaleB=0.98182453645217915
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.60262866551482031
FinalMaxA=0.72285738368495278
FinalMinB=-0.81818263711995454
FinalMaxB=0.31773469011561395

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_LCDPanelsBlock/TSOCorRndDoubleLCD
BlockDisplayName=Unknown
DebugBlockEntityId=129218889558356620
ModelEntityName=
DetectedSurfaceCount=2

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.93168237197096482, 0.15032941100071187, -0.026289713103324182)
NormalLocal=new Vector3D(-0.99999901201345143, -0.00084438873420769611, 0.0010863601534311929)
AxisALocal=new Vector3D(-0.00084438934772555242, 0.999999671383657, 9.1644715834041449E-07)
AxisBLocal=new Vector3D(-0.0010863779741771551, 4.3368086899420177E-19, -0.99999934555130532)
RawMinA=-0.69916033738971
RawMaxA=0.42165178184268048
RawMinB=-0.70256351331866718
RawMaxB=0.64383562644127645
OffsetA=0.018
OffsetB=0
ScaleA=1.0260089082540624
ScaleB=0.99946418558243788
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.69573588717928836
FinalMaxA=0.45422733163225887
FinalMinB=-0.70220280328322882
FinalMaxB=0.643474916405838

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_LCDPanelsBlock/TSOCorRndDoubleLCD
BlockDisplayName=Unknown
DebugBlockEntityId=129218889558356620
ModelEntityName=
DetectedSurfaceCount=2

ScreenCalibration[1]:
ScreenIndex=1
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.93711698293918744, 0.28365408044192009, 0.030603712657466531)
NormalLocal=new Vector3D(0.99999856542859267, -0.0012670449052846131, -0.0010866737412912053)
AxisALocal=new Vector3D(-0.0010866901795408557, 1.3716583237960954E-06, -0.99999934521100642)
AxisBLocal=new Vector3D(0.0012670445609957726, 0.99999922510485173, -7.4481939669676644E-09)
RawMinA=-0.64379078991894489
RawMaxA=0.7006520100427992
RawMinB=-0.83246086613288706
RawMaxB=0.28834889986124185
OffsetA=0.0045000000000000005
OffsetB=0.018
ScaleA=0.9999234375
ScaleB=1.0264803574218753
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.63923932296800889
FinalMaxA=0.7051005430918631
FinalMinB=-0.82930058773561355
FinalMaxB=0.32118862146396843

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_Cockpit/LargeBlockConsoleModuleScreens
BlockDisplayName=Unknown
DebugBlockEntityId=127044055067523339
ModelEntityName=
DetectedSurfaceCount=3

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.68810844409222471, -0.060211606135003669, -0.641916159116644)
NormalLocal=new Vector3D(1.1443647468769768E-08, 0.74942665605544467, 0.662087371268743)
AxisALocal=new Vector3D(1, -8.434216064027198E-09, 9.7488696156486673E-09)
AxisBLocal=new Vector3D(0, 0.6620874396796268, -0.74942659561726022)
RawMinA=-0.24353217271339725
RawMaxA=0.27819632573781605
RawMinB=-0.18936787960544016
RawMaxB=0.15694595983301796
OffsetA=0
OffsetB=-0.0030000000000000005
ScaleA=0.990491335639021
ScaleB=0.92352635010159945
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.24105170212373217
FinalMaxA=0.275715855148151
FinalMinB=-0.17912593794934639
FinalMaxB=0.14070401817692418

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_Cockpit/LargeBlockConsoleModuleScreens
BlockDisplayName=Unknown
DebugBlockEntityId=127044055067523339
ModelEntityName=
DetectedSurfaceCount=3

ScreenCalibration[1]:
ScreenIndex=1
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.042502105386799729, -0.086591250976657888, -0.61205661626996433)
NormalLocal=new Vector3D(1.1443647468769768E-08, 0.74942665605544467, 0.662087371268743)
AxisALocal=new Vector3D(1, -8.434216064027198E-09, 9.7488696156486673E-09)
AxisBLocal=new Vector3D(0, 0.6620874396796268, -0.74942659561726022)
RawMinA=-0.30275600145799564
RawMaxA=0.21897249699279242
RawMinB=-0.1495247134532797
RawMaxB=0.19678912600040785
OffsetA=0.0015
OffsetB=-0.0030000000000000005
ScaleA=0.99018803250217358
ScaleB=0.92331424388327155
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.29869640992325119
FinalMaxA=0.21791290545804798
FinalMinB=-0.13924604413718303
FinalMaxB=0.18051045668431118

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_Cockpit/LargeBlockConsoleModuleScreens
BlockDisplayName=Unknown
DebugBlockEntityId=127044055067523339
ModelEntityName=
DetectedSurfaceCount=3

ScreenCalibration[2]:
ScreenIndex=2
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.69005855723236642, -0.06411078640335037, -0.63750315718313288)
NormalLocal=new Vector3D(2.0408248328280244E-08, 0.74942665731601921, 0.66208736984187933)
AxisALocal=new Vector3D(1, -6.6292372205102416E-09, -4.6909613310891231E-09)
AxisBLocal=new Vector3D(-1.1329090861375992E-08, 0.66208744090697969, -0.74942659453294469)
RawMinA=-0.2801484370708735
RawMaxA=0.24158006138033977
RawMinB=-0.18348145030674068
RawMaxB=0.16283238912468848
OffsetA=0.0015000000000000005
OffsetB=-0.0045000000000000005
ScaleA=0.99003641576399715
ScaleB=0.93947100064211142
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.27604929414955254
FinalMaxA=0.24048091845901881
FinalMinB=-0.17750043522445422
FinalMaxB=0.14785137404240203

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TurretControlBlock/LargeTurretControlBlock
BlockDisplayName=Unknown
DebugBlockEntityId=75436398521990400
ModelEntityName=
DetectedSurfaceCount=4

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.12806731451669459, 0.69321475484576478, 0.050353624133623313)
NormalLocal=new Vector3D(2.6329320402746405E-08, -0.30911610677393847, 0.95102430701476937)
AxisALocal=new Vector3D(0.99999999999999989, 9.30904535050542E-09, -7.7309358792425884E-09)
AxisBLocal=new Vector3D(-1.387778753906192E-17, 0.95102432386909952, 0.30911605492002853)
RawMinA=-0.048775458717588788
RawMaxA=0.47959268387713655
RawMinB=-0.3180774766218184
RawMaxB=0.21206704307437635
OffsetA=-0.19200000000000014
OffsetB=-0.0075
ScaleA=1.8552358386673877
ScaleB=1.0440426179555464
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.46671514449615381
FinalMaxA=0.51353236965570126
FinalMinB=-0.33725195289292142
FinalMaxB=0.21624151934547939

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_TurretControlBlock/LargeTurretControlBlock
BlockDisplayName=Unknown
DebugBlockEntityId=75436398521990400
ModelEntityName=
DetectedSurfaceCount=4

ScreenCalibration[2]:
ScreenIndex=2
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.14628484797768662, 0.59949267277338159, 0.035663230888063779)
NormalLocal=new Vector3D(2.6329320402746405E-08, -0.30911610677393847, 0.95102430701476937)
AxisALocal=new Vector3D(0.99999999999999989, 9.30904535050542E-09, -7.7309358792425884E-09)
AxisBLocal=new Vector3D(-1.387778753906192E-17, 0.95102432386909952, 0.30911605492002853)
RawMinA=-0.030557925100390678
RawMaxA=0.49781021749433463
RawMinB=-0.22440445956316027
RawMaxB=0.30574006013303451
OffsetA=0
OffsetB=0
ScaleA=1
ScaleB=1
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.030557925100390682
FinalMaxA=0.49781021749433463
FinalMinB=-0.2244044595631603
FinalMaxB=0.30574006013303451

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_ButtonPanel/LargeBlockInsetButtonPanel
BlockDisplayName=Unknown
DebugBlockEntityId=85729577644483029
ModelEntityName=
DetectedSurfaceCount=3

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(1.0830618507675827, 0.42339992042825353, -0.14697270104586588)
NormalLocal=new Vector3D(1, -7.7401990620166551E-10, -2.5353971108950676E-09)
AxisALocal=new Vector3D(3.130150562672044E-09, 1, 1.7655397900916578E-09)
AxisBLocal=new Vector3D(-1.4723633783862943E-08, -1.8218660963824429E-09, 1)
RawMinA=-0.64431081461236128
RawMaxA=0.12563451900049169
RawMinB=-0.50459912523827744
RawMaxB=0.799525814900013
OffsetA=-0.0015000000000000005
OffsetB=4.3368086899420177E-19
ScaleA=1.0352242460658849
ScaleB=1.0535005883671746
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.65937118655659077
FinalMaxA=0.1376948909447212
FinalMinB=-0.53948485103912991
FinalMaxB=0.83441154070086543

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
Grid Schematics manual block cursor calibration values.
ChatGPT/Codex agent instructions:
Incorporate these values into PanelCursorCalibrationTable.cs at Data/Scripts/GridSchematics/PanelCursorCalibrationTable.cs.
Add a new lookup item unless BlockDefinitionId is already present. If it is already present, update or add only the matching ScreenIndex entry and preserve other screen indexes for the same block.
These values are local to the clicked block/root entity basis and include the cursor depth/z offset from the detected screen plane.

Block ID/Name:
BlockDefinitionId=MyObjectBuilder_ButtonPanel/LargeBlockInsetButtonPanel
BlockDisplayName=Unknown
DebugBlockEntityId=85729577644483029
ModelEntityName=
DetectedSurfaceCount=3

ScreenCalibration[2]:
ScreenIndex=2
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.75436842436299967, 0.1329884529406809, -0.93512890619179234)
NormalLocal=new Vector3D(1.7529557365925542E-08, 6.6708584131234613E-11, -1)
AxisALocal=new Vector3D(2.3561308266808886E-09, 1, 1.0382526146279988E-11)
AxisBLocal=new Vector3D(1, -8.6736179373388457E-19, 2.7052659075169144E-10)
RawMinA=-0.23436785404846594
RawMaxA=0.18313655245800678
RawMinB=-0.0099987616538237331
RawMaxB=0.70641867112287993
OffsetA=0.009
OffsetB=0
ScaleA=1.0257732648753146
ScaleB=1.0346695579894329
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.23074807987621709
FinalMaxA=0.19751677828575792
FinalMinB=-0.022417699518970047
FinalMaxB=0.71883760898802618

Suggested permanent lookup shape:
BlockDefinitionId => ScreenIndex => { CalibrationMode, SeedLocal, NormalLocal, AxisALocal, AxisBLocal, FinalMinA, FinalMaxA, FinalMinB, FinalMaxB, CursorDepthOffset }
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_Cockpit/LargeBlockCockpitSeat
BlockDisplayName=Unknown
DebugBlockEntityId=76040721419062051
ModelEntityName=
DetectedSurfaceCount=6

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.010902874852696124, 0.011291483778818369, -0.41471745268304211)
NormalLocal=new Vector3D(-1.75296634213528E-08, 0.29226754636955604, 0.95633659416500372)
AxisALocal=new Vector3D(1, -1.0386111666896021E-09, 2.1329075551305194E-09)
AxisBLocal=new Vector3D(8.9236997010977427E-09, 0.95633660426669187, -0.29226751331554557)
RawMinA=-0.23196104845448784
RawMaxA=0.21017759984935352
RawMinB=-0.24703223813704694
RawMaxB=0.057599876299319466
OffsetA=0
OffsetB=0
ScaleA=0.91516517155090538
ScaleB=0.87608953612321094
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.21320667026470236
FinalMaxA=0.19142322165956804
FinalMinB=-0.22815868483125831
FinalMaxB=0.038726322993530821
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_Cockpit/LargeBlockCockpitSeat
BlockDisplayName=Unknown
DebugBlockEntityId=76040721419062051
ModelEntityName=
DetectedSurfaceCount=6

ScreenCalibration[1]:
ScreenIndex=1
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.44235114551856847, -0.088348560853351146, -0.3088656511787527)
NormalLocal=new Vector3D(-0.19991491830198121, 0.28658115785143373, 0.93696598946004639)
AxisALocal=new Vector3D(0.9798132612098579, 0.0584722181813006, 0.19117262580783173)
AxisBLocal=new Vector3D(-1.3877787492350742E-17, 0.95626997176161421, -0.29248545452217189)
RawMinA=-0.083373304634711182
RawMaxA=0.12074193229222414
RawMinB=-0.080290825724886536
RawMaxB=0.088136787764377209
OffsetA=0
OffsetB=0
ScaleA=1
ScaleB=1
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.083373304634711182
FinalMaxA=0.12074193229222416
FinalMinB=-0.080290825724886536
FinalMaxB=0.088136787764377209
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_Cockpit/LargeBlockCockpitSeat
BlockDisplayName=Unknown
DebugBlockEntityId=76040721419062051
ModelEntityName=
DetectedSurfaceCount=6

ScreenCalibration[2]:
ScreenIndex=2
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.44574969912970219, -0.093220440536825516, -0.32479407299957352)
NormalLocal=new Vector3D(-0.19991491830198121, 0.28658115785143373, 0.93696598946004639)
AxisALocal=new Vector3D(0.9798132612098579, 0.0584722181813006, 0.19117262580783173)
AxisBLocal=new Vector3D(-1.3877787492350742E-17, 0.95626997176161421, -0.29248545452217189)
RawMinA=-0.083373304634711182
RawMaxA=0.12074193229222414
RawMinB=-0.080290825724886536
RawMaxB=0.088136787764377209
OffsetA=-0.048000000000000022
OffsetB=-0.018
ScaleA=1.38014514532862
ScaleB=1.38014514532862
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.17017001283739897
FinalMaxA=0.1115386404949119
FinalMinB=-0.13030429552850092
FinalMaxB=0.10215025756799161
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_Cockpit/LargeBlockCockpitSeat
BlockDisplayName=Unknown
DebugBlockEntityId=76040721419062051
ModelEntityName=
DetectedSurfaceCount=6

ScreenCalibration[3]:
ScreenIndex=3
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.44235114551856847, -0.088348560853351146, -0.3088656511787527)
NormalLocal=new Vector3D(-0.19991491830198121, 0.28658115785143373, 0.93696598946004639)
AxisALocal=new Vector3D(0.9798132612098579, 0.0584722181813006, 0.19117262580783173)
AxisBLocal=new Vector3D(-1.3877787492350742E-17, 0.95626997176161421, -0.29248545452217189)
RawMinA=-0.083373304634711182
RawMaxA=0.12074193229222414
RawMinB=-0.080290825724886536
RawMaxB=0.088136787764377209
OffsetA=0
OffsetB=0
ScaleA=1
ScaleB=1
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.083373304634711182
FinalMaxA=0.12074193229222416
FinalMinB=-0.080290825724886536
FinalMaxB=0.088136787764377209
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_Cockpit/LargeBlockCockpitSeat
BlockDisplayName=Unknown
DebugBlockEntityId=76040721419062051
ModelEntityName=
DetectedSurfaceCount=6

ScreenCalibration[4]:
ScreenIndex=4
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.010891724014527831, 0.016261896180348987, -0.39846235632245058)
NormalLocal=new Vector3D(-2.0185707166104463E-08, 0.2922675503203932, 0.95633659295758211)
AxisALocal=new Vector3D(1, 7.5251082306561168E-09, 2.0109289547572798E-09)
AxisBLocal=new Vector3D(1.387778749045971E-17, 0.95633660277820709, -0.29226751818605812)
RawMinA=-0.23196104845448784
RawMaxA=0.21017759984935352
RawMinB=-0.24703223813704694
RawMaxB=0.057599876299319466
OffsetA=0
OffsetB=0
ScaleA=1
ScaleB=1
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.23196104845448784
FinalMaxA=0.21017759984935352
FinalMinB=-0.24703223813704694
FinalMaxB=0.057599876299319452
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_Cockpit/LargeBlockCockpitSeat
BlockDisplayName=Unknown
DebugBlockEntityId=76040721419062051
ModelEntityName=
DetectedSurfaceCount=6

ScreenCalibration[5]:
ScreenIndex=5
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.41304311097379587, -0.089427333071448331, -0.31477259004623259)
NormalLocal=new Vector3D(0.20160115387922042, 0.28573412454156982, 0.93686337575285217)
AxisALocal=new Vector3D(0.9794676945039, -0.0588118731510227, -0.19283204868403728)
AxisBLocal=new Vector3D(0, 0.95650258401849453, -0.29172385361149811)
RawMinA=-0.16420955887870059
RawMaxA=0.15584695321356765
RawMinB=-0.1538097828998751
RawMaxB=0.11639883219784801
OffsetA=0
OffsetB=0
ScaleA=1
ScaleB=1
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.16420955887870059
FinalMaxA=0.15584695321356767
FinalMinB=-0.1538097828998751
FinalMaxB=0.116398832197848
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_Cockpit/SmallBlockCockpit
BlockDisplayName=Unknown
DebugBlockEntityId=76472192741453097
ModelEntityName=
DetectedSurfaceCount=4

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.030332107774373517, -0.057317423973262782, -0.2455114120566137)
NormalLocal=new Vector3D(2.1028891624206446E-08, 0.29141890557249983, 0.95659553703481481)
AxisALocal=new Vector3D(1, -4.9580908485509612E-09, -1.050556071689306E-08)
AxisBLocal=new Vector3D(0, 0.956595530289041, -0.29141892771580363)
RawMinA=-0.10077140465119235
RawMaxA=0.16143561679207813
RawMinB=-0.13171999331431009
RawMaxB=0.11655490552450937
OffsetA=0
OffsetB=0
ScaleA=0.875955390048468
ScaleB=0.86082504484616407
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.0845087208004506
FinalMaxA=0.14517293294133637
FinalMinB=-0.11444316935845215
FinalMaxB=0.099278081568651436
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_TextPanel/ATL_ArmorBlockLCD_SG
BlockDisplayName=Unknown
DebugBlockEntityId=143320605067770482
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.026200304745733642, -0.011546617490878921, -0.24187482374650426)
NormalLocal=new Vector3D(-3.278207703232447E-09, -1.4871654782263476E-08, -0.99999999999999989)
AxisALocal=new Vector3D(1, -2.5484264043551867E-09, -2.7755576824697848E-17)
AxisBLocal=new Vector3D(6.9388945666686147E-18, -1, 0)
RawMinA=-0.198886276709985
RawMaxA=0.25123479504745255
RawMinB=-0.23660478052709555
RawMaxB=0.21351626795124559
OffsetA=0
OffsetB=0
ScaleA=1.0445223535791233
ScaleB=1.0445223535791233
CursorDepthOffset=-6.0715321659188248E-18
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.20890650146508427
FinalMaxA=0.26125501980255184
FinalMinB=-0.24662500476397475
FinalMaxB=0.22353649218812477
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_TextPanel/ATL_SlopedArmorBlockLCD_SG
BlockDisplayName=Unknown
DebugBlockEntityId=89954184918153760
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.027636252140386303, 0.023507379220893344, -0.00069136304233623777)
NormalLocal=new Vector3D(-1.4781156953606102E-08, 0.7071068502737794, -0.70710671209930864)
AxisALocal=new Vector3D(0.99999999999999978, 1.0899782501330911E-08, 1.1959449220591646E-08)
AxisBLocal=new Vector3D(2.7755575141725948E-17, -0.70710678670575378, -0.70710677566734126)
RawMinA=-0.19744061751915304
RawMaxA=0.25268048252959391
RawMinB=-0.20897519295932654
RawMaxB=0.24124901622795125
OffsetA=0
OffsetB=0
ScaleA=1.0444423823364275
ScaleB=1.0444423823364275
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.20744284453218292
FinalMaxA=0.26268270954262379
FinalMinB=-0.2189797111802349
FinalMaxB=0.25125353444885962
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_TextPanel/ATL_HalfSlopeArmorBlockBaseLCD_SG
BlockDisplayName=Unknown
DebugBlockEntityId=114830857499885140
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.029180921550844077, 0.07462673029110331, -0.10678844802961565)
NormalLocal=new Vector3D(-3.1662998687976223E-08, 0.44716220962597214, -0.89445288209185014)
AxisALocal=new Vector3D(0.99999999999999978, 1.4606420716274034E-08, -5.90975423562262E-09)
AxisBLocal=new Vector3D(-1.3877787104621117E-17, -0.89445292503261242, -0.44716212373210207)
RawMinA=-0.25424147447001183
RawMaxA=0.19587962806033005
RawMinB=-0.15023937605156798
RawMaxB=0.29998339373716754
OffsetA=0
OffsetB=0
ScaleA=1.0444423823364275
ScaleB=1.0444423823364275
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.26424370153818566
FinalMaxA=0.2058818551285039
FinalMinB=-0.16024386228732618
FinalMaxB=0.30998787997292571
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_TextPanel/ATL_HalfSlopeArmorBlockTipLCD_SG
BlockDisplayName=Unknown
DebugBlockEntityId=77839036247053725
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.0046256763941996794, -0.04788906979597659, 0.081951419661501532)
NormalLocal=new Vector3D(-1.9212389818687617E-08, 0.44710429390184181, -0.894481833451376)
AxisALocal=new Vector3D(1, 9.03786653932833E-09, 5.2261719259384527E-09)
AxisBLocal=new Vector3D(2.7755574209069665E-17, -0.89448187638380761, -0.44710420801073053)
RawMinA=-0.22969733757502014
RawMaxA=0.22042377003783117
RawMinB=-0.28722856890330273
RawMaxB=0.1630163757511843
OffsetA=0
OffsetB=0
ScaleA=1.0435631026683447
ScaleB=1.0435631026683447
CursorDepthOffset=-0.0007500000000000204
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.23950167358708396
FinalMaxA=0.23022810604989502
FinalMinB=-0.29703560227824605
FinalMaxB=0.17282340912612765
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_TextPanel/ATL_SingleLCDPanel_SG
BlockDisplayName=Unknown
DebugBlockEntityId=95669028564026618
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.026149719465464395, -0.0040842634431942065, 0.23422674111882225)
NormalLocal=new Vector3D(-2.2411307115291918E-08, 1.9379083708153241E-08, -0.99999999999999956)
AxisALocal=new Vector3D(1, 4.4792543626593329E-10, 2.7755574805754424E-17)
AxisBLocal=new Vector3D(-4.3368083450858632E-18, -1, 0)
RawMinA=-0.25121993060740083
RawMaxA=0.19890117690491202
RawMinB=-0.2291458125146679
RawMaxB=0.22097531889674163
OffsetA=0
OffsetB=0
ScaleA=1.0440426179555469
ScaleB=1.0440426179555469
CursorDepthOffset=-0.00075000000000000175
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.261132186593347
FinalMaxA=0.20881343289085824
FinalMinB=-0.23905806902690349
FinalMaxB=0.23088757540897722
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_TextPanel/SmallLCDPanelWide
BlockDisplayName=Unknown
DebugBlockEntityId=143006220604521791
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.21152831213723131, 0.13373515266959207, -0.10865271787811072)
NormalLocal=new Vector3D(2.2411307115291918E-08, 1.9379083708153241E-08, 0.99999999999999956)
AxisALocal=new Vector3D(1, -4.4792543626593329E-10, 2.7755574805754424E-17)
AxisBLocal=new Vector3D(-4.3368083450858632E-18, 1, 0)
RawMinA=-1.2666899584889422
RawMaxA=1.6897788784777457
RawMinB=-0.87641374287994378
RawMaxB=0.59498481260831537
OffsetA=0
OffsetB=0.009
ScaleA=1.0086727675781251
ScaleB=0.99984688086181639
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-1.2795103420264331
FinalMaxA=1.7025992620152368
FinalMinB=-0.86730109324057325
FinalMaxB=0.60387216296894486
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_TextPanel/SmallFullBlockLCDPanel
BlockDisplayName=Unknown
DebugBlockEntityId=117202794383078929
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(0.25200978738535196, -0.010761697724974102, -0.0095362805328028134)
NormalLocal=new Vector3D(1, -3.2830719074566565E-09, -1.6146270740381824E-09)
AxisALocal=new Vector3D(0, 1.4477581908696898E-09, -1)
AxisBLocal=new Vector3D(3.4694470103615782E-18, 0.99999999999999989, 0)
RawMinA=-0.21954347731264465
RawMaxA=0.240746571678015
RawMinB=-0.23925882172569418
RawMaxB=0.24874116986154843
OffsetA=-0.0195
OffsetB=0.006
ScaleA=1.0713610725033886
ScaleB=0.96516106401382806
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.25546687309195
FinalMaxA=0.23766996745732036
FinalMinB=-0.22475812149161398
FinalMaxB=0.24624046962746823
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_TextPanel/SmallDiagonalLCDPanel
BlockDisplayName=Unknown
DebugBlockEntityId=72413618754438491
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.052976511833590825, 0.066988087349755893, -0.057234625863210709)
NormalLocal=new Vector3D(0.70710683696052923, 3.0174296005679539E-08, -0.70710672541256092)
AxisALocal=new Vector3D(-1.6508549323197942E-08, 0.99999999999999978, -1.5884709231646527E-08)
AxisBLocal=new Vector3D(0.70710680372790669, 0, 0.70710675864518779)
RawMinA=-0.305870060871424
RawMaxA=0.10522569935806984
RawMinB=-0.27595034690857917
RawMaxB=0.31359107867000657
OffsetA=0.031500000000000007
OffsetB=0.05700000000000003
ScaleA=1.1490455681516756
ScaleB=1.1792921056461776
CursorDepthOffset=-0.00075
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.30500606144549891
FinalMaxA=0.16736169993214481
FinalMinB=-0.27180040868739613
FinalMaxB=0.42344114044882358
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_TextPanel/SmallLCDPanel
BlockDisplayName=Unknown
DebugBlockEntityId=85047957861586964
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.05413942804134126, 0.23202408673577465, -0.10939382152375766)
NormalLocal=new Vector3D(3.2199071365191058E-08, 3.8281908675787987E-08, 0.99999999999999878)
AxisALocal=new Vector3D(1, -4.8278980431796965E-09, 1.0296315813210213E-10)
AxisBLocal=new Vector3D(8.673616258181557E-19, 1, -1.06074450771927E-09)
RawMinA=-0.67451327260820315
RawMaxA=0.78927979925134362
RawMinB=-0.96074897161029749
RawMaxB=0.4966965324751631
OffsetA=-0.0030000000000000005
OffsetB=-4.3368086899420177E-19
ScaleA=1.0085955410693572
ScaleB=1.0083638970171709
CursorDepthOffset=-0.001
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.6838043193413077
FinalMaxA=0.79257084598444827
FinalMinB=-0.96684393366245225
FinalMaxB=0.50279149452731786
----- Grid Schematics panel calibration -----
GridSchematicsPanelCursorCalibrationBlock
BlockDefinitionId=MyObjectBuilder_TextPanel/TransparentLCDSmall
BlockDisplayName=Unknown
DebugBlockEntityId=82099930436536417
ModelEntityName=
DetectedSurfaceCount=1

ScreenCalibration[0]:
ScreenIndex=0
CalibrationMode=PolygonDiscovery
SeedLocal=new Vector3D(-0.012352850378037048, 0.14946114109199993, -0.23562397665761414)
NormalLocal=new Vector3D(4.5551782768084561E-06, 1.4774540848192476E-05, 0.9999999998804816)
AxisALocal=new Vector3D(0.99999999981244214, 1.8836081560721576E-05, -4.5075714011066641E-06)
AxisBLocal=new Vector3D(-1.8840606886067959E-05, 0.99999999971381892, -1.4744274828760045E-05)
RawMinA=-0.23048459990929546
RawMaxA=0.23468830606307162
RawMinB=-0.39607653936755005
RawMaxB=-0.16466831228384551
OffsetA=0.010499999999999999
OffsetB=0.13050000000000009
ScaleA=0.93925523241724984
ScaleB=1.8859663750229221
CursorDepthOffset=0.0042499999999999561
ManualWidthA=0.5
ManualHeightB=0.3
FinalMinA=-0.20585618987975349
FinalMaxA=0.23105989603352964
FinalMinB=-0.36808649341746535
FinalMaxB=0.068341641766070022
";
    }
}
