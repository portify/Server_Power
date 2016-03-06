$POWER_INPUT_NONE = 0;
$POWER_INPUT_ONE  = 1;
$POWER_INPUT_TWO  = 3;
$POWER_INPUT_MANY = 2;

$POWER_OUTPUT_NONE = 0;
$POWER_OUTPUT_ONE  = 1;
$POWER_OUTPUT_TWO  = 3;
$POWER_OUTPUT_MANY = 2;

function FxDTSBrick::getTotalPowerInput(%this)
{
  %amount = 0;

  if (isObject(%this.powerInputs))
  {
    for (%i = %this.powerInputs.getCount() - 1; %i >= 0; %i--)
      %amount += %this.powerInputAmount[%this.powerInputs.getObject(%i)];
  }

  return %amount;
}

function FxDTSBrickData::getPowerDebug(%this, %obj)
{
  %in = %obj.getTotalPowerInput();
  %out = %this.getPowerOutput(%obj);

  %outputs = isObject(%obj.powerOutputs) ? %obj.powerOutputs.getCount() : 0;
  return %out SPC "W" @ (%outputs > 1 ? " / " @ %outputs : "");
}

function FxDTSBrick::updatePowerDebug(%this)
{
  if (!isObject(%this.emptyShape))
  {
    %this.emptyShape = new StaticShape()
    {
      datablock = EmptyShapeData;
    };

    %this.emptyShape.setTransform(vectorAdd(%this.getPosition(), "0 0" SPC %this.getDataBlock().brickSizeZ * 0.2 + 0.2));
  }

  %this.emptyShape.setShapeName(%this.getDataBlock().getPowerDebug(%this));
}

function FxDTSBrickData::getPowerOutput(%this, %obj)
{
  return 0;
}

function FxDTSBrickData::onPowerInputChange(%this, %obj)
{
}

function FxDTSBrick::allowsPowerInput(%this)
{
  %data = %this.getDataBlock();

  if (!%data.isPowerUnit)
    return false;

  if (%data.powerModeInput == $POWER_INPUT_MANY)
    return true;

  if (%data.powerModeInput == $POWER_INPUT_ONE)
    return !isObject(%this.powerInputs) || %this.powerInputs.getCount() < 1;

  if (%data.powerModeInput == $POWER_INPUT_TWO)
    return !isObject(%this.powerInputs) || %this.powerInputs.getCount() < 2;

  return false;
}

function FxDTSBrick::allowsPowerOutput(%this, %ignoreCount)
{
  %data = %this.getDataBlock();

  if (!%data.isPowerUnit)
    return false;

  if (%data.powerModeOutput == $POWER_INPUT_MANY)
    return true;

  if (%data.powerModeOutput == $POWER_INPUT_ONE)
    return %ignoreCount || !isObject(%this.powerOutputs) || %this.powerOutputs.getCount() < 1;

  if (%data.powerModeOutput == $POWER_INPUT_TWO)
    return %ignoreCount || !isObject(%this.powerOutputs) || %this.powerOutputs.getCount() < 2;

  return false;
}

function createPowerShape(%a, %b)
{
  %part1 = createShape(CylinderGlowShapeData, "1 0 0 1");
  %part1.transformLine(%a, %b, 0.1);

  %part2 = createShape(CubeGlowShapeData, "0 1 1 1");
  %part2.transformCube(vectorAdd(%a, vectorScale(vectorSub(%b, %a), 0.9)), 0.3);

  %group = new SimGroup();

  DebugSet.add(%group);
  MissionCleanup.add(%group);

  %group.add(%part1);
  %group.add(%part2);

  return %group;
}

function FxDTSBrick::addPowerOutput(%this, %other)
{
  if (!isObject(%other))
    return;

  %this = %this.getID();
  %other = %other.getID();

  if (!isObject(%this.powerOutputs))
    %this.powerOutputs = new SimSet();

  if (!isObject(%other.powerInputs))
    %other.powerInputs = new SimSet();

  if (%this.powerOutputs.isMember(%other) || %other.powerInputs.isMember(%this))
    return;

  %this.powerOutputs.add(%other);
  %other.powerInputs.add(%this);

  %shape = createPowerShape(%this.getWorldBoxCenter(), %other.getWorldBoxCenter());

  %this.powerOutputShape[%other] = %shape;
  %other.powerInputShape[%this] = %shape;

  %other.powerInputAmount[%this] = 0;

  updatePower(%this);
}

function FxDTSBrick::removePowerOutput(%this, %other, %cheap)
{
  if (
    !isObject(%this.powerOutputs) || !%this.powerOutputs.isMember(%other) ||
    !isObject(%other.powerInputs) || !%other.powerInputs.isMember(%this))
    return;

  %this.powerOutputs.remove(%other);
  %other.powerInputs.remove(%this);

  %shape = %this.powerOutputShape[%other];

  if (!isObject(%shape))
    %shape = %other.powerInputShape[%this];

  if (isObject(%shape))
    %shape.delete();

  %this.powerOutputShape[%other] = "";
  %other.powerInputShape[%this] = "";

  %notify = %other.powerInputAmount[%this] > 0;

  %other.powerInputAmount[%this] = "";

  if (!%cheap)
    updatePower(%this);

  updatePower(%other);

  if (%notify)
    %other.getDataBlock().onPowerInputChange(%other);
}

function FxDTSBrick::clearPowerOutputs(%this)
{
  if (isObject(%this.powerOutputs))
  {
    for (%i = %this.powerOutputs.getCount() - 1; %i >= 0; %i--)
      %this.removePowerOutput(%this.powerOutputs.getObject(%i));
  }
}

function FxDTSBrick::removePowerInput(%this, %other, %cheap)
{
  if (
    !isObject(%this.powerInputs) || !%this.powerInputs.isMember(%other) ||
    !isObject(%other.powerOutputs) || !%other.powerOutputs.isMember(%this))
    return;

  %this.powerInputs.remove(%other);
  %other.powerOutputs.remove(%this);

  %shape = %this.powerInputShape[%other];

  if (!isObject(%shape))
    %shape = %other.powerOutputShape[%this];

  if (isObject(%shape))
    %shape.delete();

  %this.powerInputShape[%other] = "";
  %other.powerOutputShape[%this] = "";

  %notify = %this.powerInputAmount[%other] > 0;

  %this.powerInputAmount[%other] = "";

  updatePower(%other);

  if (!%cheap)
  {
    if (%notify)
      %this.getDataBlock().onPowerInputChange(%this);

    updatePower(%this);
  }
}

//
// function FxDTSBrick::findPowerInput(%this, %other)
// {
//   for (%i = 0; %i < %this.powerInputCount; %i++)
//   {
//     if (%this.powerInputUnit[%i] == %other)
//       return %i;
//   }
//
//   return -1;
// }
//
// function FxDTSBrick::addPowerOutput(%this, %obj)
// {
//   if (!isObject(%obj))
//     return;
//
//   %wire = new SimGroup();
//
//   %p1 = %this.getWorldBoxCenter();
//   %p2 = %obj.getWorldBoxCenter();
//
//   %wire1 = createShape(CylinderGlowShapeData, "1 0 0 1");
//   %wire1.transformLine(%p1, %p2, 0.1);
//   %wire.add(%wire1);
//
//   %wire2 = createShape(CubeGlowShapeData, "0 1 1 1");
//   %pc = vectorAdd(%p1, vectorScale(vectorSub(%p2, %p1), 0.9));
//   %wire2.transformCube(%pc, 0.3);
//   %wire.add(%wire2);
//
//   DebugSet.add(%wire);
//   MissionCleanup.add(%wire);
//
//   if (%this.powerOutputCount $= "")
//     %this.powerOutputCount = 0;
//
//   %this.powerOutputUnit[%this.powerOutputCount] = %obj;
//   %this.powerOutputWire[%this.powerOutputCount] = %wire;
//   %this.powerOutputCount++;
//
//   if (%obj.powerInputCount $= "")
//     %obj.powerInputCount = 0;
//
//   %obj.powerInputUnit[%obj.powerInputCount] = %this;
//   %obj.powerInputAmount[%obj.powerInputCount] = 0;
//   %obj.powerInputCount++;
//
//   updatePower(%this);
// }
//
// function FxDTSBrick::clearPowerOutputs(%this)
// {
//   for (%i = %this.powerOutputCount - 1; %i >= 0; %i--)
//     %this.removePowerOutputIndex(%i, true);
// }
//
// function FxDTSBrick::killPowerOutputIndex(%this, %index)
// {
//   serverPlay3D(PowerShortCircuitSound, %this.getWorldBoxCenter());
//   %this.removePowerOutputIndex(%index);
// }

// function FxDTSBrick::removePowerInput(%this, %index, %skipOther)
// {
//   if (isObject(%this.powerInputWire[%index]))
//     %this.powerInputWire[%index].delete();
//
//   %other = %this.powerInputUnit[%index];
//
//   if (!%skipOther && isObject(%other))
//   {
//     %otherIndex = %other.findPowerOutput(%this);
//
//     if (%otherIndex != -1)
//       %other.removePowerOutput(%otherIndex, true);
//   }
//
//   %this.powerInputCount--;
//
//   %this.powerInputUnit[%index] = %this.powerInputUnit[%this.powerInputCount];
//   %this.powerInputWire[%index] = %this.powerInputWire[%this.powerInputCount];
//   %this.powerInputAmount[%index] = %this.powerInputAmount[%this.powerInputCount];
//
//   %this.powerInputUnit[%this.powerInputCount] = "";
//   %this.powerInputWire[%this.powerInputCount] = "";
//   %this.powerInputAmount[%this.powerInputCount] = "";
// }

// function FxDTSBrick::removePowerOutputUnit(%this, %unit, %skipSelf, %skipRecv)
// {
//   for (%i = 0; %i < %this.powerOutputCount; %i++)
//   {
//     if (%this.powerOutputUnit[%i] == %unit)
//     {
//       %this.removePowerOutputIndex(%i, %skipSelf, %skipRecv);
//       return true;
//     }
//   }
//
//   return false;
// }
//
// function FxDTSBrick::removePowerOutputIndex(%this, %index, %skipSelf, %skipRecv)
// {
//   %obj = %this.powerOutputUnit[%index];
//
//   if (isObject(%obj))
//   {
//     %found = false;
//
//     for (%j = 0; %j < %obj.powerInputCount; %j++)
//     {
//       if (%found)
//       {
//         %obj.powerInputUnit[%j] = %obj.powerInputUnit[%j + 1];
//         %obj.powerInputAmount[%j] = %obj.powerInputAmount[%j + 1];
//       }
//       else if (%obj.powerInputUnit[%j] == %this)
//       {
//         %amount = %obj.powerInputAmount[%j];
//         %found = true;
//         %j--;
//       }
//     }
//
//     if (%found)
//     {
//       %obj.powerInputCount--;
//
//       %obj.powerInputUnit[%obj.powerUnitCount] = "";
//       %obj.powerInputAmount[%obj.powerUnitCount] = "";
//
//       if (%amount > 0)
//         %obj.getDataBlock().onPowerInputChange(%obj);
//     }
//
//     if (!%skipRecv)
//       updatePower(%obj);
//   }
//
//   %wire = %this.powerOutputWire[%index];
//
//   if (isObject(%wire))
//     %wire.delete();
//
//   %this.powerOutputUnit[%index] = "";
//   %this.powerOutputWire[%index] = "";
//
//   %this.powerOutputCount--;
//
//   for (%j = %index; %j < %this.powerOutputCount; %j++)
//   {
//     %this.powerOutputUnit[%j] = %this.powerOutputUnit[%j + 1];
//     %this.powerOutputWire[%j] = %this.powerOutputWire[%j + 1];
//   }
//
//   %this.powerOutputUnit[%this.powerOutputCount] = "";
//   %this.powerOutputWire[%this.powerOutputCount] = "";
//
//   if (!%skipSelf)
//     updatePower(%this);
// }

function FxDTSBrick::powerCleanUp(%obj)
{
  if (isObject(%obj.emptyShape))
    %obj.emptyShape.delete();

  if (isObject(%obj.powerOutputs))
  {
    for (%i = %obj.powerOutputs.getCount() - 1; %i >= 0; %i--)
      %obj.removePowerOutput(%obj.powerOutputs.getObject(%i), true);
  }

  if (isObject(%obj.powerInputs))
  {
    for (%i = %obj.powerInputs.getCount() - 1; %i >= 0; %i--)
      %obj.removePowerInput(%obj.powerInputs.getObject(%i), true);
  }
}

package PowerPackage
{
  function FxDTSBrickData::onRemove(%this, %obj)
  {
    Parent::onRemove(%this, %obj);
    // talk("onRemove");

    %obj.powerCleanUp();
    //
    // if (%obj.powerOutputCount > 0)
    //   %obj.clearPowerOutputs();
    //
    // for (%i = %obj.powerInputCount - 1; %i >= 0; %i--)
    // {
    //   %unit = %obj.powerInputUnit[%i];
    //
    //   if (isObject(%unit))
    //     %unit.removePowerOutputUnit(%obj, false, true);
    // }
  }

  function FxDTSBrickData::onDeath(%this, %obj)
  {
    Parent::onDeath(%this, %obj);
    // talk("onDeath");
    %obj.powerCleanUp();
  }
};

activatePackage("PowerPackage");

// setLogMode(1);

function updatePower(%startUnit)
{
  // talk("power tick starting from " @ %startUnit.getDataBlock().uiName);
  // talk("updatePower(" @ %startUnit @ ")");
  // echo("updatePower(" @ %startUnit @ ")");

  %queue = 0;
  %queue0 = %startUnit;
  %queuedUnit[%startUnit] = true;

  while (%queue >= 0)
  {
    %sender = %queue[%queue];
    %queue--;

    %visitedUnit[%sender] = true;
    %amount = %sender.getDataBlock().getPowerOutput(%sender);
    // talk("   :: " @ %sender SPC %sender.getDataBlock().uiName SPC "(output " @ %amount @ " W)");
    // echo("   :: " @ %sender SPC %sender.getDataBlock().uiName SPC "(output " @ %amount @ " W)");

    %outputs = isObject(%sender.powerOutputs) ? %sender.powerOutputs.getCount() : 0;

    %sender.updatePowerDebug();

    if (%amount > 0 && %outputs > 1)
      %amount /= %outputs;

    // echo("         " @ %sender.powerOutputCount @ " outputs");

    // for (%i = %sender.powerOutputCount - 1; %i >= 0; %i--)
    for (%i = %outputs - 1; %i >= 0; %i--)
    {
      // %recipient = %sender.powerOutputUnit[%i];
      %recipient = %sender.powerOutputs.getObject(%i);

      if (%visitedUnit[%recipient])
      {
        // %sender.killPowerOutputIndex(%i);
        serverPlay3D(PowerShortCircuitSound, %recipient.getWorldBoxCenter());
        %p = new Projectile()
        {
          datablock = RadioWaveProjectile;
          initialPosition = vectorScale(vectorAdd(%sender.getWorldBoxCenter(), %recipient.getWorldBoxCenter()), 0.5);
          initialVelocity = "0 0";
        };
        %p.explode();
        %sender.removePowerOutput(%recipient);
        continue;
      }

      // %inputIndex = %recipient.findPowerInput(%sender);

      // if (%inputIndex != -1 && %recipient.powerInputAmount[%inputIndex] != %amount)
      // if (%inputIndex != -1)
      // {
      //   %recipient.powerInputAmount[%inputIndex] = %amount;
      //   %recipient.getDataBlock().onPowerInputChange(%recipient);
      //
      //   if (!%queuedUnit[%recipient])
      //   {
      //     %queue[%queue++] = %recipient;
      //     %queuedUnit[%recipient] = true;
      //   }
      // }

      if (%amount != %recipient.powerInputAmount[%sender])
      {
        %recipient.powerInputAmount[%sender] = %amount;
        %recipient.getDataBlock().onPowerInputChange(%recipient);

        if (!%queuedUnit[%recipient])
        {
          %queue[%queue++] = %recipient;
          %queuedUnit[%recipient] = true;
        }
      }
    }
  }
}
