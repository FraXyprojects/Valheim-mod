using System;
using System.Collections.Generic;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public static class NarrativeDatabase
    {
        private static readonly Random Rnd = new Random();

        // 1. Exploration and Biomes
        private static readonly string[] SingleBiomeExploration =
        {
            "Hlavním dějištěm této výpravy se stal biom {0}.",
            "Skupina věnovala většinu času průzkumu biomu {0}.",
            "Kroky hrdinů neomylně vedly do biomu {0}, kde strávili podstatnou část svého času.",
            "Tato session byla zcela zasvěcena tajemstvím biomu {0}."
        };

        private static readonly string[] MultiBiomeExploration =
        {
            "Výprava se tentokrát vydala na dlouhou cestu; od {0} až do {1}, takže měla jasný průzkumný oblouk.",
            "Dobrodruzi urazili obrovskou vzdálenost, když přešli z biomu {0} až do {1}.",
            "Příběh této session nás zavede na výpravu napříč světem, počínaje v {0} a konče v drsném {1}."
        };

        // 2. Base building and Expansion
        private static readonly string[] CampNew =
        {
            "Na mapě přibyl nový opěrný bod: {0}{1}.",
            "V pustině vyrostlo nové útočiště, hrdě se tyčící jako {0}{1}.",
            "Skupina založila novou základnu: {0}{1} poslouží jako cenná opora do budoucna.",
            "Surové prostředí ustoupilo lidské vůli, když byl položen základ pro nový {0}{1}."
        };

        private static readonly string[] CampUpgrade =
        {
            "Dříve známý {0}{1} se posunul na úroveň {2}.",
            "Původně skromný {0}{1} se dočkal obrovského rozšíření a stal se z něj pevný {2}.",
            "Tvrdá práce přinesla své ovoce a z {0}{1} vyrostl mohutnější {2}."
        };

        private static readonly string[] CampAddedAdvancedStation =
        {
            "Staré zázemí{0} dostalo další řemeslnou infrastrukturu a začalo působit jako důležitější základna.",
            "Do zázemí{0} přibyla pokročilá řemeslná stanice, což výrazně posílilo jeho význam.",
            "Hrdinové věnovali čas vylepšení své staré základny{0} o nová výrobní zařízení."
        };

        private static readonly string[] CampAddedDefenses =
        {
            "Skupina posílila jedno ze svých zavedených míst{0} o obranné prvky.",
            "Kolem základny{0} vyrostly nové obranné palisády a valy.",
            "Aby odrazili neustálé útoky, věnovali se obránci stavbě dalších obran u svého zázemí{0}."
        };

        private static readonly string[] CampExpanded =
        {
            "Známé zázemí{0} bylo během session dál rozšířeno a upevněno.",
            "Hráči věnovali pozornost svému táboru{0}, který se opět o něco rozrostl.",
            "Ozývalo se vytrvalé bušení kladiv, jak se známá základna{0} pomalu rozšiřovala."
        };

        private static readonly string[] CampGeneric =
        {
            "Stavební část výpravy vyústila v objekt typu {0}{1}, se zhruba {2} postavenými díly.",
            "Ruce stavitelů se nezastavily, a tak vznikla stavba {0}{1} čítající zhruba {2} konstrukcí.",
            "Hrubá síla surovin se proměnila v ladnou stavbu: {0}{1} o přibližně {2} částech."
        };

        // 3. Combat intensity
        private static readonly string[] CombatExtreme =
        {
            "Skupina strávila velkou část výpravy bojem o přežití a tlak nepřátel určoval tempo celé session.",
            "Byly to hodiny prolité krve a potu; přežití samo se stalo největší výzvou, neboť nepřátelé nedali skupině vydechnout.",
            "Bitevní vřava neutichala. Nepřátelský nápor byl extrémní a boj o každý metr země určoval rytmus celé výpravy."
        };

        private static readonly string[] CombatHigh =
        {
            "Velká část výpravy se nesla ve znamení souvislého boje a neustálého hlídání prostoru.",
            "Ticho bylo často přerušováno třeskem zbraní; boj byl tentokrát opravdu intenzivní.",
            "Skupina sice netancovala přímo na okraji propasti, ale tvrdé boje je zdržovaly téměř na každém kroku."
        };

        private static readonly string[] CombatMedium =
        {
            "Postup výpravy opakovaně přerušoval odpor místních nepřátel.",
            "Cesta nebyla snadná a občasné střety s nepřáteli udržovaly všechny v napětí.",
            "I přes několik ostrých výměn názorů s místní faunou šlo spíše o střídavé potyčky než souvislou bitvu."
        };

        private static readonly string[] CombatLow =
        {
            "Výprava zůstala převážně klidná a boj tvořil spíš okrajové epizody.",
            "Tentokrát bohové dopřáli bojovníkům trochu klidu. Zbraně převážně zůstaly v pochvách.",
            "Den ubíhal v relativním poklidu, střety byly spíše vzácné a krátké."
        };

        // 4. Survival & Death
        private static readonly string[] SurvivalHeroicEscape =
        {
            "{0} se nejméně jednou dostal na hranici jisté smrti, ale dokázal pokračovat v boji i po kritickém zranění.",
            "Byly to momenty, kdy u {0} jen o vlásek unikl smrti; hrdinský únik, o kterém se budou vyprávět zkazky.",
            "S těžkými zraněními, ale živý – {0} pohlédl smrti do očí a vrátil se zpět do boje."
        };

        private static readonly string[] SurvivalLastStand =
        {
            "Jeden z nejtvrdších střetů měl charakter posledního odporu: {0} přežil s minimem sil a krátce nato dál porážel nepřátele.",
            "Ocitli se zády ke zdi. {0} odolal brutálnímu náporu s vypětím posledních sil a přežil.",
            "Zoufalý boj se šťastným koncem – {0} ukázal, že poslední vzdor dokáže odvrátit i zdánlivě jistou porážku."
        };

        private static readonly string[] SurvivalNearDeath =
        {
            "{0} unikl smrti jen s minimem sil; nejnižší zachycené zdraví kleslo na {1}.",
            "Při jedné děsivé potyčce zdraví bojovníka {0} kleslo až na {1}. Osud však stál při něm.",
            "{0} utrpěl hrozivý úder, kdy mu zbývalo pouhých {1} života, ale nakonec přežil."
        };

        private static readonly string[] SurvivalNoDeathsHighStress =
        {
            "Navzdory dlouhodobému tlaku a opakovaným zraněním výprava přežila bez ztráty života.",
            "I přes obrovské vypětí a množství krve prolité v bitvách se nikdo nevracel přes pohřebiště bohů.",
            "Tlak byl obrovský, rány bolestivé, a přesto celá skupina přežila bez jediného úmrtí."
        };

        private static readonly string[] SurvivalMediumStress =
        {
            "Výprava čelila opakovanému nebezpečí, které postupně zvedalo tlak na přežití.",
            "Bylo to nebezpečné dobrodružství, kde chyby bolely, ale skupina držela pevně pohromadě."
        };

        private static readonly string[] SurvivalWithDeaths =
        {
            "Výprava měla i fázi obnovy po smrti, po které bylo potřeba znovu získat tempo.",
            "Bohužel, Valhalla tentokrát přivítala své padlé. Výprava se musela vzpamatovat z utrpěných ztrát.",
            "Smrt nebyla výpravě cizí. Cesta zpět pro ztracené vybavení stála čas i nervy."
        };

        // 5. Progression
        private static readonly string[] ProgressionPhase =
        {
            "Podle zachycených zásob a stanic už svět nese znaky fáze {0}.",
            "Všechny dostupné znaky a milníky ukazují na to, že hrdinové naplno prožívají fázi {0}.",
            "Technologický pokrok a zdroje jasně odrážejí, že se svět nachází v etapě zvané {0}."
        };

        // 6. Discovery Operations
        private static readonly string[] ResourceOperation =
        {
            "Zásobovací linku session nejvíc určovala oblast '{0}'.",
            "Soustředěné úsilí se během cesty vyprofilovalo do masivní operace s názvem '{0}'.",
            "Místo pouhého bloudění nabrala výprava jasný směr – {0} se stala klíčovým motivem."
        };

        // 7. Boss Kills
        private static readonly string[] BossNoDeath =
        {
            "Boss byl poražen bez jediné zaznamenané smrti.",
            "Souboj s bossem proběhl jako dobře nacvičená symfonie mečů; hrozba padla, aniž by někdo zaplatil životem.",
            "Impozantní vítězství! Boss padl k zemi a žádný z hrdinů nezemřel."
        };

        private static readonly string[] BossWithDeath =
        {
            "Boss fight se stal jedním z hlavních zlomů celé session.",
            "Bitva s obávaným bossem si vybrala krutou daň a stala se určujícím bodem celé výpravy.",
            "Souboj proti prastarému zlu byl chaotický a neobešel se bez obětí."
        };

        public static string GetRandom(string[] options, params object[] args)
        {
            if (options == null || options.Length == 0) return string.Empty;
            string template = options[Rnd.Next(options.Length)];
            return args != null && args.Length > 0 ? string.Format(template, args) : template;
        }

        public static string GetSingleBiomeExploration(string biome) => GetRandom(SingleBiomeExploration, biome);
        public static string GetMultiBiomeExploration(string first, string last) => GetRandom(MultiBiomeExploration, first, last);
        public static string GetCampNew(string name, string biome) => GetRandom(CampNew, name, biome);
        public static string GetCampUpgrade(string prev, string biome, string newTier) => GetRandom(CampUpgrade, prev, biome, newTier);
        public static string GetCampAddedAdvancedStation(string biome) => GetRandom(CampAddedAdvancedStation, biome);
        public static string GetCampAddedDefenses(string biome) => GetRandom(CampAddedDefenses, biome);
        public static string GetCampExpanded(string biome) => GetRandom(CampExpanded, biome);
        public static string GetCampGeneric(string name, string biome, int count) => GetRandom(CampGeneric, name, biome, count);
        public static string GetCombatExtreme() => GetRandom(CombatExtreme);
        public static string GetCombatHigh() => GetRandom(CombatHigh);
        public static string GetCombatMedium() => GetRandom(CombatMedium);
        public static string GetCombatLow() => GetRandom(CombatLow);
        public static string GetSurvivalHeroicEscape(string player) => GetRandom(SurvivalHeroicEscape, player);
        public static string GetSurvivalLastStand(string player) => GetRandom(SurvivalLastStand, player);
        public static string GetSurvivalNearDeath(string player, string healthPercent) => GetRandom(SurvivalNearDeath, player, healthPercent);
        public static string GetSurvivalNoDeathsHighStress() => GetRandom(SurvivalNoDeathsHighStress);
        public static string GetSurvivalMediumStress() => GetRandom(SurvivalMediumStress);
        public static string GetSurvivalWithDeaths() => GetRandom(SurvivalWithDeaths);
        public static string GetProgressionPhase(string phaseLabel) => GetRandom(ProgressionPhase, phaseLabel);
        public static string GetResourceOperation(string operationName) => GetRandom(ResourceOperation, operationName);
        public static string GetBossNoDeath() => GetRandom(BossNoDeath);
        public static string GetBossWithDeath() => GetRandom(BossWithDeath);
    }
}
