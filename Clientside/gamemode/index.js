const NativeUI = require('nativeui');
const Menu = NativeUI.Menu;
const UIMenuItem = NativeUI.UIMenuItem;
const Point = NativeUI.Point;

var currentCheckpoint = null;

mp.events.add('showCarSelectMenu', () => {
    const ui = new Menu("CarSelecht", "Select a Car", new Point(1500, 50));
    ui.AddItem(new UIMenuItem(
        "T20",
        "T20",
    ));
    ui.AddItem(new UIMenuItem(
        "issi6",
        "issi6",
    ));
    ui.AddItem(new UIMenuItem(
        "bf400",
        "bf400",
    ));
    ui.ItemSelect.on(item => {
        mp.events.callRemote('PLAYER_RACE_CAR_SELECTED', item.Text);
        ui.Close();
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

mp.events.add("playerEnterCheckpoint", () => {
    mp.events.callRemote('PLAYER_PASSED_CHECKPOINT');    
});

mp.events.add("destroyActiveCheckpoint", () => {
    currentCheckpoint.destroy();
    currentCheckpoint = null;
});