datablock ItemData(WireToolItem)
{
  shapeFile = "base/data/shapes/printGun.dts";

  doColorShift = true;
  colorShiftColor = "0.4 0.55 0.6 1";

  uiName = "Wire Tool";
  image = WireToolImage;
  canDrop = true;
};

datablock ShapeBaseImageData(WireToolImage)
{
  shapeFile = WireToolItem.shapeFile;

  doColorShift = WireToolItem.doColorShift;
  colorShiftColor = WireToolItem.colorShiftColor;

  item = WireToolItem;
  armReady = true;
};

function WireToolImage::onUnMount(%this, %obj, %slot)
{
  %obj.wireToolConnect = 0;
}

function Player::wireToolTrigger(%this, %slot, %state)
{
  if (!%state)
    return;

  %a = %this.getEyePoint();
  %b = vectorAdd(%a, vectorScale(%this.getEyeVector(), 50));

  %mask = $TypeMasks::FxBrickObjectType;
  %ray = containerRayCast(%a, %b, %mask);

  if (%ray)
    %col = %ray.getID();
  else
    %col = 0;

  if (%slot == 0 || %slot == 4)
  {
    if (isObject(%this.wireToolConnect))
    {
      if (%col && %col != %this.wireToolConnect && %col.allowsPowerInput())
      {
        if (%slot != 4 || !%this.wireToolConnect.allowsPowerOutput())
          %this.wireToolConnect.clearPowerOutputs();
        %this.wireToolConnect.addPowerOutput(%col);
      }

      %this.wireToolConnect = 0;
    }
    else
    {
      if (%col && %col.allowsPowerOutput(%slot != 4))
      {
        if (%slot != 4)
          %col.clearPowerOutputs();
        %this.wireToolConnect = %col;
      }
    }
  }
}

package WireToolPackage
{
  function Armor::onTrigger(%this, %obj, %slot, %state)
  {
    if (%obj.getMountedImage(0) == WireToolImage.getID())
      %obj.wireToolTrigger(%slot, %state);
    else
      Parent::onTrigger(%this, %obj, %slot, %state);
  }
};

activatePackage("WireToolPackage");
