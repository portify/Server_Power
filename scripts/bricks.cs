datablock FxDTSBrickData(BrickPowerNodeData)
{
  category = "Special";
  subCategory = "Power";
  uiName = "Power Node";

  brickSizeX = 1;
  brickSizeY = 1;
  brickSizeZ = 1;

  isPowerUnit = true;
  powerModeInput = $POWER_INPUT_ONE;
  powerModeOutput = $POWER_OUTPUT_ONE;
};

function BrickPowerNodeData::getPowerOutput(%this, %obj)
{
  return %obj.getTotalPowerInput();
}

datablock FxDTSBrickData(BrickPowerCombinatorData)
{
  category = "Special";
  subCategory = "Power";
  uiName = "Combiner";

  brickSizeX = 1;
  brickSizeY = 1;
  brickSizeZ = 5;

  isPowerUnit = true;
  powerModeInput = $POWER_INPUT_TWO;
  powerModeOutput = $POWER_OUTPUT_ONE;
};

function BrickPowerCombinatorData::getPowerOutput(%this, %obj)
{
  return %obj.getTotalPowerInput();
}

datablock FxDTSBrickData(BrickPowerSplitterData)
{
  category = "Special";
  subCategory = "Power";
  uiName = "Splitter";

  brickSizeX = 1;
  brickSizeY = 1;
  brickSizeZ = 5;

  isPowerUnit = true;
  powerModeInput = $POWER_INPUT_ONE;
  powerModeOutput = $POWER_OUTPUT_TWO;
};

function BrickPowerSplitterData::getPowerOutput(%this, %obj)
{
  return %obj.getTotalPowerInput();
}

datablock FxDTSBrickData(BrickPowerLogicAndData)
{
  category = "Special";
  subCategory = "Power";
  uiName = "Logic - And";

  brickSizeX = 1;
  brickSizeY = 1;
  brickSizeZ = 5;

  isPowerUnit = true;
  powerModeInput = $POWER_INPUT_MANY;
  powerModeOutput = $POWER_OUTPUT_ONE;
};

function BrickPowerLogicAndData::getPowerOutput(%this, %obj)
{
  %amount = 0;

  if (isObject(%obj.powerInputs))
  {
    for (%i = %obj.powerInputs.getCount() - 1; %i >= 0; %i--)
    {
      %a = %obj.powerInputAmount[%obj.powerInputs.getObject(%i)];

      if (%a <= 0)
        return 0;

      %amount += %a;
    }
  }

  return %amount;
}

datablock FxDTSBrickData(BrickPowerGenerator100WData)
{
  category = "Special";
  subCategory = "Power";
  uiName = "Generator - 100W";

  brickSizeX = 2;
  brickSizeY = 2;
  brickSizeZ = 6;

  isPowerUnit = true;
  powerModeInput = $POWER_INPUT_NONE;
  powerModeOutput = $POWER_OUTPUT_ONE;
};

function BrickPowerGenerator100WData::getPowerOutput(%this, %obj)
{
  return 100;
}

datablock FxDTSBrickData(BrickPowerLightData)
{
  category = "Special";
  subCategory = "Power";
  uiName = "Light";

  brickSizeX = 1;
  brickSizeY = 1;
  brickSizeZ = 3;

  isPowerUnit = true;
  powerModeInput = $POWER_INPUT_ONE;
  powerModeOutput = $POWER_OUTPUT_NONE;
};

function BrickPowerLightData::onPowerInputChange(%this, %obj)
{
  %in = %obj.getTotalPowerInput();
  %obj.setLight(%in > 150 ? BrightLight : (%in > 50 ? PlayerLight : 0));
}

datablock FxDTSBrickData(BrickPowerBattery1Data)
{
  category = "Special";
  subCategory = "Power";
  uiName = "Battery 1";

  brickSizeX = 2;
  brickSizeY = 2;
  brickSizeZ = 9;

  isPowerUnit = true;
  powerModeInput = $POWER_INPUT_ONE;
  powerModeOutput = $POWER_OUTPUT_ONE;

  batteryCapacity = 10000;
  batteryOutput = 100;
};

function BrickPowerBattery1Data::onPowerInputChange(%this, %obj)
{
  Parent::onPowerInputChange(%this, %obj);

  if (!isEventPending(%obj.powerBatteryTick))
    %obj.powerBatteryTick();
}

function BrickPowerBattery1Data::getPowerDebug(%this, %obj)
{
  return Parent::getPowerDebug(%this, %obj) @ " (" @ (%obj.storedEnergy) @ " J)";
}

function BrickPowerBattery1Data::getPowerOutput(%this, %obj)
{
  return getMin(%obj.storedEnergy, %this.batteryOutput);
}

function FxDTSBrick::powerBatteryTick(%this)
{
  cancel(%this.powerBatteryTick);

  %data = %this.getDataBlock();
  %prev = %this.storedEnergy + 0;

  if (isObject(%this.powerOutputs) && %this.powerOutputs.getCount() >= 1)
    %this.storedEnergy = getMax(0, %this.storedEnergy - %data.batteryOutput * 0.1);

  %this.storedEnergy = getMin(%data.batteryCapacity, %this.storedEnergy + %this.getTotalPowerInput() * 0.1);

  %curr = %this.storedEnergy;

  if (getMin(%curr, %data.batteryOutput) != getMin(%prev, %data.batteryOutput))
    updatePower(%this);

  if (%curr != %prev)
    %this.updatePowerDebug();

  %this.powerBatteryTick = %this.schedule(100, "powerBatteryTick");
}

datablock FxDTSBrickData(BrickPowerSwitchData)
{
  category = "Special";
  subCategory = "Power";
  uiName = "Switch";

  brickSizeX = 1;
  brickSizeY = 1;
  brickSizeZ = 3;

  isPowerUnit = true;
  powerModeInput = $POWER_INPUT_ONE;
  powerModeOutput = $POWER_OUTPUT_ONE;
};

function BrickPowerSwitchData::getPowerOutput(%this, %obj)
{
  return %obj.getTotalPowerInput() * %obj.switchState;
}

function BrickPowerSwitchData::onActivate(%this, %obj)
{
  %obj.switchState = !%obj.switchState;
  %obj.setColorFX(%obj.switchState ? 3 : 0);
  updatePower(%obj);
}

datablock FxDTSBrickData(BrickPowerPressurePlateData)
{
  category = "Special";
  subCategory = "Power";
  uiName = "Pressure Plate";

  brickSizeX = 4;
  brickSizeY = 4;
  brickSizeZ = 1;

  isPowerUnit = true;
  powerModeInput = $POWER_INPUT_ONE;
  powerModeOutput = $POWER_OUTPUT_ONE;
};

function BrickPowerPressurePlateData::getPowerOutput(%this, %obj)
{
  return %obj.getTotalPowerInput() * %obj.pressurePlateState;
}

function BrickPowerPressurePlateData::onPlayerTouch(%this, %obj)
{
  cancel(%obj.pressurePlateSchedule);
  %obj.pressurePlateState = true;
  %obj.pressurePlateSchedule = %obj.schedule(300, "pressurePlateDisable");
  updatePower(%obj);
}

function BrickPowerPressurePlateData::onAdd(%this, %obj)
{
  %obj.enableTouch = true;
  echo("add");
}

function FxDTSBrick::pressurePlateDisable(%this)
{
  %this.pressurePlateState = false;
  updatePower(%this);
}

function FxDTSBrickData::onActivate() { }

package PowerBrickPackage
{
  function FxDTSBrick::onActivate(%this, %a, %b, %c, %d, %e)
  {
    %this.getDataBlock().onActivate(%this);
    return Parent::onActivate(%this, %a, %b, %c, %d, %e);
  }

  function FxDTSBrick::onPlayerTouch(%this, %a, %b, %c, %d, %e)
  {
    %this.getDataBlock().onPlayerTouch(%this);
    return Parent::onPlayerTouch(%this, %a, %b, %c, %d, %e);
  }
};

activatePackage("PowerBrickPackage");
