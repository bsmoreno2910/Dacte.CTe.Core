using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Cte.Core.Structs;

namespace Cte.Core.Esquemas
{
    /// <summary>
    /// CT-e processado (XML autorizado).
    /// </summary>
    [XmlType(Namespace = Namespaces.CTe)]
    [XmlRoot("cteProc", Namespace = Namespaces.CTe, IsNullable = false)]
    public class ProcCTe
    {
        public CTe CTe { get; set; }

        public ProtCTe protCTe { get; set; }

        [XmlAttribute]
        public string versao { get; set; }
    }

    /// <summary>
    /// Identificação do Ambiente.
    /// </summary>
    [Serializable]
    [XmlType(Namespace = Namespaces.CTe)]
    public enum TAmb
    {
        [XmlEnum("1")] Producao = 1,
        [XmlEnum("2")] Homologacao = 2,
    }

    /// <summary>
    /// CT-e completo (modelo 57 / 67 / 64 — todos compartilham a estrutura).
    /// </summary>
    [Serializable]
    [XmlType(Namespace = Namespaces.CTe)]
    public class CTe
    {
        public InfCte infCte { get; set; }

        /// <summary>Informações suplementares (QR Code do DACTE).</summary>
        public InfCTeSupl infCTeSupl { get; set; }
    }

    /// <summary>
    /// Informações suplementares do CT-e — contém o QR Code para consulta
    /// pública no portal SEFAZ.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class InfCTeSupl
    {
        public string qrCodCTe { get; set; }
    }

    /// <summary>
    /// Informações do CT-e.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class InfCte
    {
        [XmlAttribute(DataType = "ID")]
        public string Id { get; set; }

        [XmlAttribute]
        public string versao { get; set; }

        public Ide ide { get; set; }
        public Compl compl { get; set; }
        public Emitente emit { get; set; }
        public Empresa rem { get; set; }
        public Empresa exped { get; set; }
        public Empresa receb { get; set; }
        public Empresa dest { get; set; }
        public VPrest vPrest { get; set; }
        public Imp imp { get; set; }
        public InfCTeNorm infCTeNorm { get; set; }

        [XmlElement("autXML")]
        public List<AutXml> autXML { get; set; } = new List<AutXml>();
    }

    /// <summary>
    /// Identificação do CT-e.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Ide
    {
        public string cUF { get; set; }
        public string cCT { get; set; }

        /// <summary>CFOP - Natureza da Operação.</summary>
        public string CFOP { get; set; }
        public string natOp { get; set; }
        public string forPag { get; set; }

        /// <summary>57 = CT-e; 67 = CT-e OS; 64 = GTV-e.</summary>
        public int mod { get; set; }
        public int serie { get; set; }

        /// <summary>Número do CT-e.</summary>
        public long nCT { get; set; }

        public DateTimeOffsetIso8601 dhEmi { get; set; }

        /// <summary>Formato de impressão. 1=Retrato, 2=Paisagem.</summary>
        public int tpImp { get; set; }

        /// <summary>Forma de emissão. 1=Normal, 4=EPEC, 5=FS-DA, 7=Autorização SVC-RS, 8=SVC-SP.</summary>
        public int tpEmis { get; set; }

        public string cDV { get; set; }

        public TAmb tpAmb { get; set; }

        /// <summary>Tipo do CT-e. 0=Normal, 1=Complemento de Valores, 2=Anulação, 3=Substituto.</summary>
        public int tpCTe { get; set; }

        /// <summary>1=Normal; 2=Recolhimento Metropolitano; 3=Excesso de Bagagem; 4=Fretamento; 5=Transporte de Pessoas; 6=Multimodal; 9=GTV.</summary>
        public int procEmi { get; set; }

        public string verProc { get; set; }

        public DateTimeOffsetIso8601? dhCont { get; set; }
        public string xJust { get; set; }

        public string cMunEnv { get; set; }
        public string xMunEnv { get; set; }
        public string UFEnv { get; set; }

        /// <summary>Modal: 01=Rodoviário, 02=Aéreo, 03=Aquaviário, 04=Ferroviário, 05=Dutoviário, 06=Multimodal.</summary>
        public string modal { get; set; }

        /// <summary>Tipo do Serviço. 0=Normal, 1=Subcontratação, 2=Redespacho, 3=Redespacho Intermediário, 4=Serviço Vinculado a Multimodal.</summary>
        public int tpServ { get; set; }

        public string cMunIni { get; set; }
        public string xMunIni { get; set; }
        public string UFIni { get; set; }

        public string cMunFim { get; set; }
        public string xMunFim { get; set; }
        public string UFFim { get; set; }

        /// <summary>Indicador "tomador é estrangeiro". 1 = sim.</summary>
        public string retira { get; set; }
        public string xDetRetira { get; set; }

        /// <summary>Indicador IE do tomador. 1=Contribuinte, 2=Isento, 9=Não Contribuinte.</summary>
        public int? indIEToma { get; set; }

        /// <summary>Indicador CT-e Globalizado. 1 = sim.</summary>
        public string indGlobalizado { get; set; }

        /// <summary>Tomador é uma das pessoas referenciadas: 0=Remetente, 1=Expedidor, 2=Recebedor, 3=Destinatário.</summary>
        public Toma3 toma3 { get; set; }

        /// <summary>Tomador externo às pessoas referenciadas (4=Outros). Carrega CNPJ/CPF, IE, xNome, xFant, fone, endereço.</summary>
        public Toma4 toma4 { get; set; }
    }

    /// <summary>
    /// Tomador entre os participantes (rem/exped/receb/dest).
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Toma3
    {
        /// <summary>0=Remetente; 1=Expedidor; 2=Recebedor; 3=Destinatário.</summary>
        public int toma { get; set; }
    }

    /// <summary>
    /// Tomador "Outros" (4) com dados completos.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Toma4
    {
        public int toma { get; set; }
        public string CNPJ { get; set; }
        public string CPF { get; set; }
        public string IE { get; set; }
        public string xNome { get; set; }
        public string xFant { get; set; }
        public string fone { get; set; }
        public Endereco enderToma { get; set; }
        public string email { get; set; }
    }

    /// <summary>
    /// Endereço usado pelas várias entidades do CT-e. Inclui telefone, que no CT-e
    /// fica dentro do grupo de endereço (enderEmit/enderReme/enderToma/etc.).
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Endereco
    {
        public string xLgr { get; set; }
        public string nro { get; set; }
        public string xCpl { get; set; }
        public string xBairro { get; set; }
        public string cMun { get; set; }
        public string xMun { get; set; }
        public string CEP { get; set; }
        public string UF { get; set; }
        public string cPais { get; set; }
        public string xPais { get; set; }
        public string fone { get; set; }
    }

    /// <summary>
    /// Empresa "padrão" para Remetente, Expedidor, Recebedor e Destinatário.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Empresa
    {
        public string CNPJ { get; set; }
        public string CPF { get; set; }
        public string IE { get; set; }
        public string xNome { get; set; }
        public string xFant { get; set; }
        public string fone { get; set; }
        public string email { get; set; }
        public string ISUF { get; set; }

        [XmlElement("enderReme")] public Endereco enderReme { get; set; }
        [XmlElement("enderExped")] public Endereco enderExped { get; set; }
        [XmlElement("enderReceb")] public Endereco enderReceb { get; set; }
        [XmlElement("enderDest")] public Endereco enderDest { get; set; }

        /// <summary>
        /// Endereço "efetivo": retorna o que estiver preenchido.
        /// </summary>
        [XmlIgnore]
        public Endereco Endereco => enderReme ?? enderExped ?? enderReceb ?? enderDest;
    }

    /// <summary>
    /// Emitente (com IM, CRT, dados próprios).
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Emitente
    {
        public string CNPJ { get; set; }
        public string IE { get; set; }
        public string IEST { get; set; }
        public string xNome { get; set; }
        public string xFant { get; set; }

        public Endereco enderEmit { get; set; }

        /// <summary>Código de Regime Tributário. 1=SN, 2=SN-Excesso, 3=Regime Normal.</summary>
        public int CRT { get; set; }
    }

    /// <summary>
    /// Informações complementares do CT-e (xObs, xCaracAd, xCaracSer, xEmi, fluxo, Entrega, etc.).
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Compl
    {
        public string xCaracAd { get; set; }
        public string xCaracSer { get; set; }
        public string xEmi { get; set; }
        public string xObs { get; set; }

        public Fluxo fluxo { get; set; }
        public Entrega Entrega { get; set; }

        [XmlElement("ObsCont")] public List<ObsCont> ObsCont { get; set; } = new List<ObsCont>();
        [XmlElement("ObsFisco")] public List<ObsFisco> ObsFisco { get; set; } = new List<ObsFisco>();
    }

    /// <summary>
    /// Periodo ou horario programado de entrega da carga.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Entrega
    {
        public DestEnde destEnde { get; set; }
        public NoPeriodo noPeriodo { get; set; }
        public ComHora comHora { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class DestEnde
    {
        public string CNPJ { get; set; }
        public string CPF { get; set; }
        public string xNome { get; set; }
        public Endereco enderDest { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class NoPeriodo
    {
        public string tpPer { get; set; }
        public string dIni { get; set; }
        public string dFim { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class ComHora
    {
        public string tpHor { get; set; }
        public string hProg { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class ObsCont
    {
        [XmlAttribute] public string xCampo { get; set; }
        public string xTexto { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class ObsFisco
    {
        [XmlAttribute] public string xCampo { get; set; }
        public string xTexto { get; set; }
    }

    /// <summary>
    /// Previsão do fluxo da carga.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Fluxo
    {
        public string xOrig { get; set; }

        [XmlElement("pass")] public List<Pass> pass { get; set; } = new List<Pass>();

        public string xDest { get; set; }
        public string xRota { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Pass
    {
        public string xPass { get; set; }
    }

    /// <summary>
    /// Valores da prestação do serviço.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class VPrest
    {
        public decimal vTPrest { get; set; }
        public decimal vRec { get; set; }

        [XmlElement("Comp")]
        public List<Comp> Comp { get; set; } = new List<Comp>();
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Comp
    {
        public string xNome { get; set; }
        public decimal vComp { get; set; }
    }

    /// <summary>
    /// Imposto. ICMS é uma escolha entre vários tipos (00/20/45/60/90/SN/OutraUF).
    /// Modela como um "container" achatado com todos os campos opcionais e usamos o que tiver.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Imp
    {
        public ICMS ICMS { get; set; }

        /// <summary>Grupo IBS/CBS da Reforma Tributária 2026.</summary>
        public IBSCBS IBSCBS { get; set; }

        public decimal? vTotTrib { get; set; }
        public decimal? vTotDFe { get; set; }
        public string infAdFisco { get; set; }
    }

    /// <summary>
    /// Reforma Tributária 2026: imposto sobre Bens e Serviços (IBS) +
    /// Contribuição sobre Bens e Serviços (CBS).
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class IBSCBS
    {
        public string CST { get; set; }
        public string cClassTrib { get; set; }
        public GIBSCBS gIBSCBS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class GIBSCBS
    {
        public decimal? vBC { get; set; }
        public GIBSUF gIBSUF { get; set; }
        public GIBSMun gIBSMun { get; set; }
        public decimal? vIBS { get; set; }
        public GCBS gCBS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class GIBSUF
    {
        public decimal? pIBSUF { get; set; }
        public decimal? vIBSUF { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class GIBSMun
    {
        public decimal? pIBSMun { get; set; }
        public decimal? vIBSMun { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class GCBS
    {
        public decimal? pCBS { get; set; }
        public decimal? vCBS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class ICMS
    {
        public ICMS00 ICMS00 { get; set; }
        public ICMS20 ICMS20 { get; set; }
        public ICMS45 ICMS45 { get; set; }
        public ICMS60 ICMS60 { get; set; }
        public ICMS90 ICMS90 { get; set; }
        public ICMSOutraUF ICMSOutraUF { get; set; }
        public ICMSSN ICMSSN { get; set; }
    }

    public abstract class ICMSBase
    {
        public string CST { get; set; }
        public decimal? vBC { get; set; }
        public decimal? pICMS { get; set; }
        public decimal? vICMS { get; set; }
        public decimal? pRedBC { get; set; }
    }

    [Serializable, XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class ICMS00 : ICMSBase { }

    [Serializable, XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class ICMS20 : ICMSBase { }

    [Serializable, XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class ICMS45 { public string CST { get; set; } }

    [Serializable, XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class ICMS60
    {
        public string CST { get; set; }
        public decimal? vBCSTRet { get; set; }
        public decimal? pICMSSTRet { get; set; }
        public decimal? vICMSSTRet { get; set; }
        public decimal? pRedBCOutraUF { get; set; }
    }

    [Serializable, XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class ICMS90 : ICMSBase { }

    [Serializable, XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class ICMSOutraUF
    {
        public string CST { get; set; }
        public decimal? pRedBCOutraUF { get; set; }
        public decimal? vBCOutraUF { get; set; }
        public decimal? pICMSOutraUF { get; set; }
        public decimal? vICMSOutraUF { get; set; }
    }

    [Serializable, XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class ICMSSN
    {
        public string CST { get; set; }
        public string indSN { get; set; }
    }

    /// <summary>
    /// Informações do CT-e Normal (não complemento, não substituição).
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class InfCTeNorm
    {
        public InfCarga infCarga { get; set; }
        public InfDoc infDoc { get; set; }
        public InfModal infModal { get; set; }

        [XmlElement("seg")]
        public List<Seg> seg { get; set; } = new List<Seg>();
    }

    /// <summary>
    /// Informações da carga.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class InfCarga
    {
        public decimal? vCarga { get; set; }
        public string proPred { get; set; }
        public string xOutCat { get; set; }

        [XmlElement("infQ")]
        public List<InfQ> infQ { get; set; } = new List<InfQ>();
    }

    /// <summary>
    /// Quantidade da carga. cUnid: 00=M3, 01=KG, 02=TON, 03=Unidade, 04=Litros, 05=mmBTU.
    /// tpMed: PESO BRUTO, PESO BASE DE CÁLCULO, PESO AFERIDO, CUBAGEM, QUANTIDADE DE VOLUMES, etc.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class InfQ
    {
        public string cUnid { get; set; }
        public string tpMed { get; set; }
        public decimal qCarga { get; set; }
    }

    /// <summary>
    /// Documentos originários (NF-e, NF, ou outros).
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class InfDoc
    {
        [XmlElement("infNFe")]
        public List<InfNFe> infNFe { get; set; } = new List<InfNFe>();

        [XmlElement("infNF")]
        public List<InfNF> infNF { get; set; } = new List<InfNF>();

        [XmlElement("infOutros")]
        public List<InfOutros> infOutros { get; set; } = new List<InfOutros>();
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class InfNFe
    {
        public string chave { get; set; }
        public string PIN { get; set; }
        public string dPrev { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class InfNF
    {
        public string serie { get; set; }
        public string nDoc { get; set; }
        public string mod { get; set; }
        public string nCFOP { get; set; }
        public decimal? vBC { get; set; }
        public decimal? vICMS { get; set; }
        public decimal? vBCST { get; set; }
        public decimal? vST { get; set; }
        public decimal? vProd { get; set; }
        public decimal? vNF { get; set; }
        public string nCT { get; set; }
        public string PIN { get; set; }
        public string dEmi { get; set; }
        public string dPrev { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class InfOutros
    {
        public string tpDoc { get; set; }
        public string descOutros { get; set; }
        public string nDoc { get; set; }
        public string dEmi { get; set; }
        public decimal? vDocFisc { get; set; }
        public string dPrev { get; set; }
    }

    /// <summary>
    /// Informações do modal. Apenas um dos modais é preenchido por CT-e.
    /// O XSD do CT-e define o conteúdo de <c>infModal</c> como <c>xs:any</c>
    /// — por isso usamos <c>XmlAnyElement</c> e deserializamos cada modal
    /// sob demanda nas propriedades <c>rodo</c>/<c>aereo</c>/etc.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class InfModal
    {
        [XmlAttribute] public string versaoModal { get; set; }

        [XmlAnyElement]
        public System.Xml.XmlElement[] AnyElements { get; set; }

        [XmlIgnore] public Rodo rodo       => DeserializeAny<Rodo>("rodo");
        [XmlIgnore] public Aereo aereo     => DeserializeAny<Aereo>("aereo");
        [XmlIgnore] public Aquav aquav     => DeserializeAny<Aquav>("aquav");
        [XmlIgnore] public Ferrov ferrov   => DeserializeAny<Ferrov>("ferrov");
        [XmlIgnore] public Duto duto       => DeserializeAny<Duto>("duto");
        [XmlIgnore] public Multimodal multimodal => DeserializeAny<Multimodal>("multimodal");

        private T DeserializeAny<T>(string elementName) where T : class
        {
            if (AnyElements == null) return null;
            foreach (var el in AnyElements)
            {
                if (string.Equals(el.LocalName, elementName, StringComparison.OrdinalIgnoreCase))
                {
                    var root = new XmlRootAttribute(elementName) { Namespace = el.NamespaceURI };
                    var ser = new XmlSerializer(typeof(T), root);
                    using (var nr = new System.Xml.XmlNodeReader(el))
                        return (T)ser.Deserialize(nr);
                }
            }
            return null;
        }
    }

    /// <summary>Modal Rodoviário. RNTRC + ordens de coleta.</summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Rodo
    {
        public string RNTRC { get; set; }
        public string CIOT { get; set; }
        public string lota { get; set; }
    }

    /// <summary>Modal Aéreo.</summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Aereo
    {
        public string nMinu { get; set; }
        public string nOCA { get; set; }
        public string dPrevAereo { get; set; }
        public string aeroOri { get; set; }

        [XmlElement("aeroPag")]
        public List<string> aeroPag { get; set; } = new List<string>();

        public string aeroDes { get; set; }
        public NatCarga natCarga { get; set; }
        public Tarifa tarifa { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class NatCarga
    {
        public string xDime { get; set; }
        public string cInfManu { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Tarifa
    {
        public string CL { get; set; }
        public string cTar { get; set; }
        public decimal? vTar { get; set; }
    }

    /// <summary>Modal Aquaviário.</summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Aquav
    {
        public decimal? vPrest { get; set; }
        public decimal? vAFRMM { get; set; }
        public string xNavio { get; set; }
        public string nViag { get; set; }
        public string direc { get; set; }
        public string irin { get; set; }

        [XmlElement("balsa")] public List<Balsa> balsa { get; set; } = new List<Balsa>();
        [XmlElement("detCont")] public List<DetCont> detCont { get; set; } = new List<DetCont>();
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Balsa { public string xBalsa { get; set; } }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class DetCont
    {
        public string nCont { get; set; }
        [XmlElement("lacre")] public List<Lacre> lacre { get; set; } = new List<Lacre>();
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Lacre { public string nLacre { get; set; } }

    /// <summary>Modal Ferroviário.</summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Ferrov
    {
        public string tpTraf { get; set; }
        public string fluxo { get; set; }
        public decimal? vFrete { get; set; }
        public string respFat { get; set; }
        public string ferrEmi { get; set; }

        [XmlElement("trafMut")] public TrafMut trafMut { get; set; }

        [XmlElement("ferroEnv")] public List<FerroEnv> ferroEnv { get; set; } = new List<FerroEnv>();
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class TrafMut { public string ferrEmi { get; set; } }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class FerroEnv
    {
        public string CNPJ { get; set; }
        public string cInt { get; set; }
        public string IE { get; set; }
        public string xNome { get; set; }
        public Endereco enderFerro { get; set; }
    }

    /// <summary>Modal Dutoviário.</summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Duto
    {
        public decimal? vTar { get; set; }
        public string dIni { get; set; }
        public string dFim { get; set; }
    }

    /// <summary>Modal Multimodal.</summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Multimodal
    {
        public string COTM { get; set; }
        public string indNegociavel { get; set; }
        public Seg seg { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class Seg
    {
        public string respSeg { get; set; }
        public InfSeg infSeg { get; set; }
        public string nApol { get; set; }
        public string nAver { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class InfSeg
    {
        public string xSeg { get; set; }
        public string CNPJ { get; set; }
    }

    /// <summary>
    /// CNPJ autorizado a baixar o XML.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class AutXml
    {
        public string CNPJ { get; set; }
        public string CPF { get; set; }
    }

    /// <summary>
    /// Protocolo de autorização do CT-e.
    /// </summary>
    [Serializable]
    [XmlType(Namespace = Namespaces.CTe)]
    public class ProtCTe
    {
        public InfProt infProt { get; set; }

        [XmlAttribute] public string versao { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.CTe)]
    public class InfProt
    {
        public TAmb tpAmb { get; set; }
        public string verAplic { get; set; }
        public string chCTe { get; set; }
        public DateTimeOffsetIso8601 dhRecbto { get; set; }
        public string nProt { get; set; }
        public string digVal { get; set; }
        public int cStat { get; set; }
        public string xMotivo { get; set; }

        [XmlAttribute(DataType = "ID")]
        public string Id { get; set; }
    }
}
