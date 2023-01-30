using WaterTank.Models;
using Microsoft.EntityFrameworkCore;
using WaterTank.Web.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WaterTank.Web.Data
{
    public class InfoBoxService : ICrud<InfoBox>
    {
        WaterTankDB db;

        public InfoBoxService()
        {
            if (db == null) db = new WaterTankDB();

        }
        public bool DeleteData(object Id)
        {
            var selData = (db.InfoBoxs.Where(x => x.Id == (long)Id).FirstOrDefault());
            db.InfoBoxs.Remove(selData);
            db.SaveChanges();
            return true;
        }

        public List<InfoBox> FindByKeyword(string Keyword)
        {
            var data = from x in db.InfoBoxs
                       where x.Message.Contains(Keyword)
                       select x;
            return data.ToList();
        }

        public List<InfoBox> GetAllData()
        {
            return db.InfoBoxs.ToList();
        }

        public InfoBox GetDataById(object Id)
        {
            return db.InfoBoxs.Where(x => x.Id == (long)Id).FirstOrDefault();
        }


        public bool InsertData(InfoBox data)
        {
            try
            {
                db.InfoBoxs.Add(data);
                db.SaveChanges();
                return true;
            }
            catch
            {

            }
            return false;

        }



        public bool UpdateData(InfoBox data)
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
            return db.InfoBoxs.Max(x => x.Id);
        }
    }

}