exec("./support/vizard.cs");
exec("./sounds/_data.cs");

datablock StaticShapeData(FrameThinData)
{
	shapeFile = "Add-Ons/Server_Power/shapes/frame_thin.dts";
};

datablock StaticShapeData(EmptyShapeData)
{
  shapeFile = "base/data/shapes/empty.dts";
};

exec("./scripts/power.cs");
exec("./scripts/bricks.cs");
exec("./scripts/tool.cs");
