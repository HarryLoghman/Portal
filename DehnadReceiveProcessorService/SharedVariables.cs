using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DehnadReceiveProcessorService
{
    public class SharedVariables
    {
        static JabehAbzarLibrary.HandleMo v_jabehAbzarLibrary;
        public static JabehAbzarLibrary.HandleMo prp_jabehAbzarLibrary
        {
            get
            {
                if (v_jabehAbzarLibrary == null)
                    v_jabehAbzarLibrary = new JabehAbzarLibrary.HandleMo();
                return v_jabehAbzarLibrary;
            }
        }

        static ShenoYad500Library.HandleMo v_shenoyad500Library;
        public static ShenoYad500Library.HandleMo prp_shenoYad500Library
        {
            get
            {
                if (v_shenoyad500Library == null) v_shenoyad500Library = new ShenoYad500Library.HandleMo();
                return v_shenoyad500Library;
            }
        }

        static AcharLibrary.HandleMo v_acharLibrary;
        public static AcharLibrary.HandleMo prp_acharLibrary
        {
            get
            {
                if (v_acharLibrary == null) v_acharLibrary = new AcharLibrary.HandleMo();
                return v_acharLibrary;
            }
        }

        static AsemanLibrary.HandleMo v_asemanLibrary;
        public static AsemanLibrary.HandleMo prp_asemanLibrary
        {
            get
            {
                if (v_asemanLibrary == null) v_asemanLibrary = new AsemanLibrary.HandleMo();
                return v_asemanLibrary;
            }
        }

        static AvvalPod500Library.HandleMo v_avvalPod500Library;
        public static AvvalPod500Library.HandleMo prp_avvalPod500Library
        {
            get
            {
                if (v_avvalPod500Library == null) v_avvalPod500Library = new AvvalPod500Library.HandleMo();
                return v_avvalPod500Library;
            }
        }

        private static TakavarLibrary.HandleMo v_takavarLibrary;

        public static TakavarLibrary.HandleMo prp_takavarLibrary
        {
            get
            {
                if (v_takavarLibrary == null) v_takavarLibrary = new TakavarLibrary.HandleMo();
                return v_takavarLibrary;
            }

        }

        private static AvvalPodLibrary.HandleMo v_avvalPodLibrary;

        public static AvvalPodLibrary.HandleMo prp_avvalPodLibrary
        {
            get
            {
                if (v_avvalPodLibrary == null) v_avvalPodLibrary = new AvvalPodLibrary.HandleMo();
                return v_avvalPodLibrary;
            }

        }

        private static TajoTakhtLibrary.HandleMo v_tajoTakhtLibrary;

        public static TajoTakhtLibrary.HandleMo prp_tajoTakhtLibrary
        {
            get
            {
                if (v_tajoTakhtLibrary == null)
                    v_tajoTakhtLibrary = new TajoTakhtLibrary.HandleMo();
                return v_tajoTakhtLibrary;
            }

        }

        private static LahzeyeAkharLibrary.HandleMo v_lahzeyeAkharLibrary;

        public static LahzeyeAkharLibrary.HandleMo prp_lahzeyeAkharLibrary
        {
            get
            {
                if (v_lahzeyeAkharLibrary == null) v_lahzeyeAkharLibrary = new LahzeyeAkharLibrary.HandleMo();
                return v_lahzeyeAkharLibrary;
            }

        }

        private static HazaranLibrary.HandleMo v_hazaranLibrary;

        public static HazaranLibrary.HandleMo prp_hazaranLibrary
        {
            get
            {
                if (v_hazaranLibrary == null) v_hazaranLibrary = new HazaranLibrary.HandleMo();
                return v_hazaranLibrary;
            }
            
        }

        private static HalgheLibrary.HandleMo v_halgheLibrary;

        public static HalgheLibrary.HandleMo prp_halgheLibrary
        {
            get
            {
                if (v_halgheLibrary == null) v_halgheLibrary = new HalgheLibrary.HandleMo();
                return v_halgheLibrary;
            }
            
        }

        private static TamlyLibrary.HandleMo v_tamlyLibrary;

        public static TamlyLibrary.HandleMo prp_tamlyLibrary
        {
            get
            {
                if (v_tamlyLibrary == null) v_tamlyLibrary = new TamlyLibrary.HandleMo();
                return v_tamlyLibrary;
            }
        }

        private static Tamly500Library.HandleMo v_tamly500Library;

        public static Tamly500Library.HandleMo prp_tamly500Library
        {
            get
            {
                if (v_tamly500Library == null) v_tamly500Library = new Tamly500Library.HandleMo();
                return v_tamly500Library;
            }
        }

        private static FitShowLibrary.HandleMo v_fitshowLibrary;

        public static FitShowLibrary.HandleMo prp_FitshowLibrary
        {
            get
            {
                if (v_fitshowLibrary == null) v_fitshowLibrary = new FitShowLibrary.HandleMo();
                return v_fitshowLibrary;
            }
        }

        private static  MenchBazLibrary.HandleMo v_menchBazLibrary;

        public static MenchBazLibrary.HandleMo prp_menchBazLibrary
        {
            get
            {
                if (v_menchBazLibrary == null) v_menchBazLibrary = new MenchBazLibrary.HandleMo();
                return v_menchBazLibrary;
            }
        }

        private static  AvvalYadLibrary.HandleMo v_avvalYadLibrary;

        public static AvvalYadLibrary.HandleMo prp_avvalYadLibrary
        {
            get
            {
                if (v_avvalYadLibrary == null) v_avvalYadLibrary = new AvvalYadLibrary.HandleMo();
                return v_avvalYadLibrary;
            }
        }

        private static BehAmooz500Library.HandleMo v_behAmooz500Library;

        public static BehAmooz500Library.HandleMo prp_behAmooz500Library
        {
            get
            {
                if (v_behAmooz500Library == null) v_behAmooz500Library = new BehAmooz500Library.HandleMo();
                return v_behAmooz500Library;
            }
        }

        private static SoratyLibrary.HandleMo v_soratyLibrary;

        public static SoratyLibrary.HandleMo prp_soratyBazLibrary
        {
            get
            {
                if (v_soratyLibrary == null) v_soratyLibrary = new SoratyLibrary.HandleMo();
                return v_soratyLibrary;
            }
        }

        private static NebulaLibrary.HandleMo v_nebulaLibrary;

        public static NebulaLibrary.HandleMo prp_nebulaBazLibrary
        {
            get
            {
                if (v_nebulaLibrary == null) v_nebulaLibrary = new NebulaLibrary.HandleMo();
                return v_nebulaLibrary;
            }
        }

        private static PhantomLibrary.HandleMo v_phantomLibrary;
        public static PhantomLibrary.HandleMo prp_phantomLibrary
        {
            get
            {
                if (v_phantomLibrary == null) v_phantomLibrary = new PhantomLibrary.HandleMo();
                return v_phantomLibrary;
            }
        }

        private static MedioLibrary.HandleMo v_medioLibrary;

        public static MedioLibrary.HandleMo prp_medioLibrary
        {
            get
            {
                if (v_medioLibrary == null) v_medioLibrary = new MedioLibrary.HandleMo();
                return v_medioLibrary;
            }
        }

        private static SoltanLibrary.HandleMo v_soltanLibrary;

        public static SoltanLibrary.HandleMo prp_soltanLibrary
        {
            get
            {
                if (v_soltanLibrary == null) v_soltanLibrary = new SoltanLibrary.HandleMo();
                return v_soltanLibrary;
            }
        }

        private static DonyayeAsatirLibrary.HandleMo v_donyayeAsatirLibrary;

        public static DonyayeAsatirLibrary.HandleMo prp_donyayeAsatirLibrary
        {
            get
            {
                if (v_donyayeAsatirLibrary == null) v_donyayeAsatirLibrary = new DonyayeAsatirLibrary.HandleMo();
                return v_donyayeAsatirLibrary;
            }
        }

        private static ShahreKalamehLibrary.HandleMo v_shahreKalamehLibrary;

        public static ShahreKalamehLibrary.HandleMo prp_shahreKalamehLibrary
        {
            get
            {
                if (v_shahreKalamehLibrary == null) v_shahreKalamehLibrary = new ShahreKalamehLibrary.HandleMo();
                return v_shahreKalamehLibrary;
            }
        }
    }
}