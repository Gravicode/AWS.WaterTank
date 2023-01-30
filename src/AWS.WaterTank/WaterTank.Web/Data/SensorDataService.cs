using WaterTank.Models;
using Microsoft.EntityFrameworkCore;

namespace WaterTank.Web.Data
{
    public class SensorDataService : ICrud<SensorData>
    {
        WaterTankDB db;

        public SensorDataService()
        {
            if (db == null) db = new WaterTankDB();

        }
        public bool DeleteData(object Id)
        {
            var selData = (db.SensorDatas.Where(x => x.Id == (long)Id).FirstOrDefault());
            db.SensorDatas.Remove(selData);
            db.SaveChanges();
            return true;
        }

        public List<SensorData> FindByKeyword(string Keyword)
        {
            var data = from x in db.SensorDatas
                       where x.Tanggal.ToString().Contains(Keyword)
                       select x;
            return data.ToList();
        }

        public List<SensorData> GetAllData()
        {
            return db.SensorDatas.ToList();
        }
        public List<SensorData> GetLatest(int Limit=25)
        {
            return db.SensorDatas.OrderByDescending(x=>x.Id).Take(Limit).ToList();
        }

        public SensorData GetDataById(object Id)
        {
            return db.SensorDatas.Where(x => x.Id == (long)Id).FirstOrDefault();
        }


        public bool InsertData(SensorData data)
        {
            try
            {
                db.SensorDatas.Add(data);
                db.SaveChanges();
                return true;
            }
            catch
            {

            }
            return false;

        }



        public bool UpdateData(SensorData data)
        {
            try
            {
                db.Entry(data).State = EntityState.Modified;
                db.SaveChanges();

                /*
                if (sel != null)
                {
                    sel.Nama = data.Nama;
                    sel.Keterangan = data.Keterangan;
                    sel.Tanggal = data.Tanggal;
                    sel.DocumentUrl = data.DocumentUrl;
                    sel.StreamUrl = data.StreamUrl;
                    return true;

                }*/
                return true;
            }
            catch
            {

            }
            return false;
        }

        public long GetLastId()
        {
            return db.SensorDatas.Max(x => x.Id);
        }
    }

}