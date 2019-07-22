const NativeUI = require('nativeui');
const Menu = NativeUI.Menu;
const UIMenuItem = NativeUI.UIMenuItem;
const Point = NativeUI.Point;

var currentCheckpoint = null;
var carSelectMenu = null;

mp.events.add('showCarSelectMenu', () => {
    if(carSelectMenu != null)
    {
        carSelectMenu.Close();
        craSelectMenu = null;
    }
    carSelectMenu = new Menu("CarSelecht", "Select a Car", new Point(1500, 50));
    carSelectMenu.AddItem(new UIMenuItem(
        "T20",
        "T20",
    ));
    carSelectMenu.AddItem(new UIMenuItem(
        "issi6",
        "issi6",
    ));
    carSelectMenu.AddItem(new UIMenuItem(
        "bf400",
        "bf400",
    ));
    carSelectMenu.ItemSelect.on(item => {
        mp.events.callRemote('PLAYER_RACE_CAR_SELECTED', item.Text);
    });
    carSelectMenu.MenuClose.on(() => {
        mp.events.callRemote('PLAYER_CAR_SELECT_CANCELED');
    });
});

mp.events.add('setClientCheckpoint', (type, position, direction) => {
    if(currentCheckpoint != null)
    {
        currentCheckpoint.destroy();
    }
    currentCheckpoint = mp.checkpoints.new(type, position, 10,
                        {
                            direction: direction,
                            color: [ 255, 255, 0, 255 ],
                            visible: true,
                            dimension: 1
                        });
})

mp.events.add("playerEnterCheckpoint",(checkpoint) => {
    mp.events.callRemote('PLAYER_PASSED_CHECKPOINT', checkpoint.position.x, checkpoint.position.y, checkpoint.position.z);    
});

mp.events.add("playerExitColshape", () => {
    if(carSelectMenu != null)
    {
        carSelectMenu.Close();
        craSelectMenu = null;
    }
});

mp.events.add("destroyActiveCheckpoint", () => {
    if(currentCheckpoint!= null)
    {
        currentCheckpoint.destroy();
        currentCheckpoint = null;
    }
});