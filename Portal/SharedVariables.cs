using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Portal
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

        private static SoltanLibrary.HandleMo v_soltanLibrary;

        public static SoltanLibrary.HandleMo prp_soltanLibrary
        {
            get
            {
                if (v_soltanLibrary == null) v_soltanLibrary = new SoltanLibrary.HandleMo();
                return v_soltanLibrary;
            }
        }


        private static AvvalYadLibrary.HandleMo v_avvalYadLibrary;

        public static AvvalYadLibrary.HandleMo prp_avvalYadLibrary
        {
            get
            {
                if (v_avvalYadLibrary == null) v_avvalYadLibrary = new AvvalYadLibrary.HandleMo();
                return v_avvalYadLibrary;
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


        private static FitShowLibrary.HandleMo v_fitshowLibrary;

        public static FitShowLibrary.HandleMo prp_FitshowLibrary
        {
            get
            {
                if (v_fitshowLibrary == null) v_fitshowLibrary = new FitShowLibrary.HandleMo();
                return v_fitshowLibrary;
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
    }
}