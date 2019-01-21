using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Portal
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

        private static SharedShortCodeServiceLibrary.HandleMo v_soltanLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_soltanLibrary
        {
            get
            {
                if (v_soltanLibrary == null) v_soltanLibrary = new SharedShortCodeServiceLibrary.HandleMo("Soltan");
                return v_soltanLibrary;
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

        private static SharedShortCodeServiceLibrary.HandleMo v_tamlyLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_tamlyLibrary
        {
            get
            {
                if (v_tamlyLibrary == null) v_tamlyLibrary = new SharedShortCodeServiceLibrary.HandleMo("Tamly");
                return v_tamlyLibrary;
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


        private static SharedShortCodeServiceLibrary.HandleMo v_donyayeAsatirLibrary;

        public static SharedShortCodeServiceLibrary.HandleMo prp_donyayeAsatirLibrary
        {
            get
            {
                if (v_donyayeAsatirLibrary == null) v_donyayeAsatirLibrary = new SharedShortCodeServiceLibrary.HandleMo("DonyayeAsatir");
                return v_donyayeAsatirLibrary;
            }
        }
    }
}