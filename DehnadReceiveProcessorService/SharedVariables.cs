using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DehnadReceiveProcessorService
{
    public class SharedVariables
    {
        static SharedShortCodeServiceLibrary.HandleMo v_jabehAbzarLibrary;
        public static SharedShortCodeServiceLibrary.HandleMo prp_jabehAbzarLibrary
        {
            get
            {
                if (v_jabehAbzarLibrary == null)
                    v_jabehAbzarLibrary = new SharedShortCodeServiceLibrary.HandleMo("JabehAbzar");
                return v_jabehAbzarLibrary;
            }
        }

        static SharedShortCodeServiceLibrary.HandleMo v_shenoyadLibrary;
        public static SharedShortCodeServiceLibrary.HandleMo prp_shenoYadLibrary
        {
            get
            {
                if (v_shenoyadLibrary == null) v_shenoyadLibrary = new SharedShortCodeServiceLibrary.HandleMo("ShenoYad");
                return v_shenoyadLibrary;
            }
        }

        static SharedShortCodeServiceLibrary.HandleMo v_shenoyad500Library;
        public static SharedShortCodeServiceLibrary.HandleMo prp_shenoYad500Library
        {
            get
            {
                if (v_shenoyad500Library == null) v_shenoyad500Library = new SharedShortCodeServiceLibrary.HandleMo("ShenoYad500");
                return v_shenoyad500Library;
            }
        }

        static SharedShortCodeServiceLibrary.HandleMo v_acharLibrary;
        public static SharedShortCodeServiceLibrary.HandleMo prp_acharLibrary
        {
            get
            {
                if (v_acharLibrary == null) v_acharLibrary = new SharedShortCodeServiceLibrary.HandleMo("Achar");
                return v_acharLibrary;
            }
        }

        static SharedShortCodeServiceLibrary.HandleMo v_hoshangLibrary;
        public static SharedShortCodeServiceLibrary.HandleMo prp_hoshangLibrary
        {
            get
            {
                if (v_hoshangLibrary == null) v_hoshangLibrary = new SharedShortCodeServiceLibrary.HandleMo("Hoshang");
                return v_hoshangLibrary;
            }
        }

        static SharedShortCodeServiceLibrary.HandleMo v_asemanLibrary;
        public static SharedShortCodeServiceLibrary.HandleMo prp_asemanLibrary
        {
            get
            {
                if (v_asemanLibrary == null) v_asemanLibrary = new SharedShortCodeServiceLibrary.HandleMo("Aseman");
                return v_asemanLibrary;
            }
        }

        static SharedShortCodeServiceLibrary.HandleMo v_avvalPod500Library;
        public static SharedShortCodeServiceLibrary.HandleMo prp_avvalPod500Library
        {
            get
            {
                if (v_avvalPod500Library == null) v_avvalPod500Library = new SharedShortCodeServiceLibrary.HandleMo("AvvalPod500");
                return v_avvalPod500Library;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_takavarLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_takavarLibrary
        {
            get
            {
                if (v_takavarLibrary == null) v_takavarLibrary = new SharedShortCodeServiceLibrary.HandleMo("Takavar");
                return v_takavarLibrary;
            }

        }

        private static SharedShortCodeServiceLibrary.HandleMo v_avvalPodLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_avvalPodLibrary
        {
            get
            {
                if (v_avvalPodLibrary == null) v_avvalPodLibrary = new SharedShortCodeServiceLibrary.HandleMo("AvvalPod");
                return v_avvalPodLibrary;
            }

        }

        private static SharedShortCodeServiceLibrary.HandleMo v_tajoTakhtLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_tajoTakhtLibrary
        {
            get
            {
                if (v_tajoTakhtLibrary == null)
                    v_tajoTakhtLibrary = new SharedShortCodeServiceLibrary.HandleMo("TajoTakht");
                return v_tajoTakhtLibrary;
            }

        }

        private static SharedShortCodeServiceLibrary.HandleMo v_lahzeyeAkharLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_lahzeyeAkharLibrary
        {
            get
            {
                if (v_lahzeyeAkharLibrary == null) v_lahzeyeAkharLibrary = new SharedShortCodeServiceLibrary.HandleMo("LahzeyeAkhar");
                return v_lahzeyeAkharLibrary;
            }

        }

        private static SharedShortCodeServiceLibrary.HandleMo v_hazaranLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_hazaranLibrary
        {
            get
            {
                if (v_hazaranLibrary == null) v_hazaranLibrary = new SharedShortCodeServiceLibrary.HandleMo("Hazaran");
                return v_hazaranLibrary;
            }

        }

        private static SharedShortCodeServiceLibrary.HandleMo v_halgheLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_halgheLibrary
        {
            get
            {
                if (v_halgheLibrary == null) v_halgheLibrary = new SharedShortCodeServiceLibrary.HandleMo("Halghe");
                return v_halgheLibrary;
            }

        }

        private static SharedShortCodeServiceLibrary.HandleMo v_tamlyLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_tamlyLibrary
        {
            get
            {
                if (v_tamlyLibrary == null) v_tamlyLibrary = new SharedShortCodeServiceLibrary.HandleMo("Tamly");
                return v_tamlyLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_tamly500Library;

        public static SharedShortCodeServiceLibrary.HandleMo prp_tamly500Library
        {
            get
            {
                if (v_tamly500Library == null) v_tamly500Library = new SharedShortCodeServiceLibrary.HandleMo("Tamly500");
                return v_tamly500Library;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_fitshowLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_FitshowLibrary
        {
            get
            {
                if (v_fitshowLibrary == null) v_fitshowLibrary = new SharedShortCodeServiceLibrary.HandleMo("FitShow");
                return v_fitshowLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_menchBazLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_menchBazLibrary
        {
            get
            {
                if (v_menchBazLibrary == null) v_menchBazLibrary = new SharedShortCodeServiceLibrary.HandleMo("MenchBaz");
                return v_menchBazLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_avvalYadLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_avvalYadLibrary
        {
            get
            {
                if (v_avvalYadLibrary == null) v_avvalYadLibrary = new SharedShortCodeServiceLibrary.HandleMo("AvvalYad");
                return v_avvalYadLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_behAmooz500Library;

        public static SharedShortCodeServiceLibrary.HandleMo prp_behAmooz500Library
        {
            get
            {
                if (v_behAmooz500Library == null) v_behAmooz500Library = new SharedShortCodeServiceLibrary.HandleMo("BehAmooz500");
                return v_behAmooz500Library;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_soratyLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_soratyBazLibrary
        {
            get
            {
                if (v_soratyLibrary == null) v_soratyLibrary = new SharedShortCodeServiceLibrary.HandleMo("Soraty");
                return v_soratyLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_defendIranLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_defendIranLibrary
        {
            get
            {
                if (v_defendIranLibrary == null) v_defendIranLibrary = new SharedShortCodeServiceLibrary.HandleMo("DefendIran");
                return v_defendIranLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_dambelLibrary;
        public static SharedShortCodeServiceLibrary.HandleMo prp_dambelLibrary
        {
            get
            {
                if (v_dambelLibrary == null) v_dambelLibrary = new SharedShortCodeServiceLibrary.HandleMo("Dambel");
                return v_dambelLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_medadLibrary;
        public static SharedShortCodeServiceLibrary.HandleMo prp_medadLibrary
        {
            get
            {
                if (v_medadLibrary == null) v_medadLibrary = new SharedShortCodeServiceLibrary.HandleMo("Medad");
                return v_medadLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_musicYadLibrary;
        public static SharedShortCodeServiceLibrary.HandleMo prp_musicYadLibrary
        {
            get
            {
                if (v_musicYadLibrary == null) v_musicYadLibrary = new SharedShortCodeServiceLibrary.HandleMo("MusicYad");
                return v_musicYadLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_porShetabLibrary;
        public static SharedShortCodeServiceLibrary.HandleMo prp_porShetabLibrary
        {
            get
            {
                if (v_porShetabLibrary == null) v_porShetabLibrary = new SharedShortCodeServiceLibrary.HandleMo("PorShetab");
                return v_porShetabLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_tahChinLibrary;
        public static SharedShortCodeServiceLibrary.HandleMo prp_tahChinLibrary
        {
            get
            {
                if (v_tahChinLibrary == null) v_tahChinLibrary = new SharedShortCodeServiceLibrary.HandleMo("TahChin");
                return v_tahChinLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_nebulaLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_nebulaLibrary
        {
            get
            {
                if (v_nebulaLibrary == null) v_nebulaLibrary = new SharedShortCodeServiceLibrary.HandleMo("Nebula");
                return v_nebulaLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_dezhbanLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_dezhbanLibrary
        {
            get
            {
                if (v_dezhbanLibrary == null) v_dezhbanLibrary = new SharedShortCodeServiceLibrary.HandleMo("Dezhban");
                return v_dezhbanLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_phantomLibrary;
        public static SharedShortCodeServiceLibrary.HandleMo prp_phantomLibrary
        {
            get
            {
                if (v_phantomLibrary == null) v_phantomLibrary = new SharedShortCodeServiceLibrary.HandleMo("Phantom");
                return v_phantomLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_medioLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_medioLibrary
        {
            get
            {
                if (v_medioLibrary == null) v_medioLibrary = new SharedShortCodeServiceLibrary.HandleMo("Medio");
                return v_medioLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_soltanLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_soltanLibrary
        {
            get
            {
                if (v_soltanLibrary == null) v_soltanLibrary = new SharedShortCodeServiceLibrary.HandleMo("Soltan");
                return v_soltanLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_donyayeAsatirLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_donyayeAsatirLibrary
        {
            get
            {
                if (v_donyayeAsatirLibrary == null) v_donyayeAsatirLibrary = new SharedShortCodeServiceLibrary.HandleMo("DonyayeAsatir");
                return v_donyayeAsatirLibrary;
            }
        }

        private static SharedShortCodeServiceLibrary.HandleMo v_shahreKalamehLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_shahreKalamehLibrary
        {
            get
            {
                if (v_shahreKalamehLibrary == null) v_shahreKalamehLibrary = new SharedShortCodeServiceLibrary.HandleMo("ShahreKalameh");
                return v_shahreKalamehLibrary;
            }
        }
    }
}