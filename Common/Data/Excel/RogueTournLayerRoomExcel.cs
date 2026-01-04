namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("RogueTournLayerRoom.json")]
public class RogueTournLayerRoomExcel : ExcelResource
{
    public int LayerID { get; set; }
    public int RoomIndex { get; set; }
    public Dictionary<int, int> Door1 { get; set; } = [];
    public Dictionary<int, int> Door2 { get; set; } = [];
    public Dictionary<int, int> Door3 { get; set; } = [];

    public override int GetId()
    {
        return LayerID;
    }

    public override void Loaded()
    {
        GameData.RogueTournLayerRoomData.TryAdd(LayerID, []);
        GameData.RogueTournLayerRoomData[LayerID][RoomIndex] = this;
    }
}