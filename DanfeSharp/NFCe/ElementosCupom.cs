using System;
using System.Drawing;
using System.Linq;
using DanfeSharp.Graphics;

namespace DanfeSharp.NFCe
{
    /// <summary>
    /// Item do fluxo vertical do cupom: mede a própria altura para uma largura e se desenha numa posição.
    /// </summary>
    internal abstract class ItemCupom
    {
        public abstract float Altura(float largura);
        public abstract void Desenhar(Gfx gfx, float x, float y, float largura);
    }

    /// <summary>
    /// Texto com quebra de linha automática.
    /// </summary>
    internal class TextoCupom : ItemCupom
    {
        private readonly String _Texto;
        private readonly Fonte _Fonte;
        private readonly AlinhamentoHorizontal _Alinhamento;

        public TextoCupom(String texto, Fonte fonte, AlinhamentoHorizontal alinhamento = AlinhamentoHorizontal.Esquerda)
        {
            _Texto = texto ?? String.Empty;
            _Fonte = fonte;
            _Alinhamento = alinhamento;
        }

        private TextBlock Criar(float largura) => new TextBlock(_Texto, _Fonte) { Width = largura, AlinhamentoHorizontal = _Alinhamento };

        public override float Altura(float largura) => String.IsNullOrEmpty(_Texto) ? 0 : Criar(largura).Height;

        public override void Desenhar(Gfx gfx, float x, float y, float largura)
        {
            if (String.IsNullOrEmpty(_Texto)) return;

            var tb = Criar(largura);
            tb.X = x;
            tb.Y = y;
            tb.Draw(gfx);
        }
    }

    /// <summary>
    /// Linha com colunas de larguras proporcionais. A altura é a da coluna mais alta.
    /// </summary>
    internal class ColunasCupom : ItemCupom
    {
        private readonly Fonte _Fonte;
        private readonly Tuple<String, float, AlinhamentoHorizontal>[] _Colunas;

        /// <param name="colunas">Texto, largura em porcentagem e alinhamento de cada coluna.</param>
        public ColunasCupom(Fonte fonte, params Tuple<String, float, AlinhamentoHorizontal>[] colunas)
        {
            _Fonte = fonte;
            _Colunas = colunas;
        }

        public static Tuple<String, float, AlinhamentoHorizontal> Coluna(String texto, float largura, AlinhamentoHorizontal alinhamento = AlinhamentoHorizontal.Esquerda)
            => Tuple.Create(texto ?? String.Empty, largura, alinhamento);

        private TextBlock[] Criar(float x, float y, float largura)
        {
            var blocos = new TextBlock[_Colunas.Length];
            float cx = x;

            for (int i = 0; i < _Colunas.Length; i++)
            {
                float w = largura * _Colunas[i].Item2 / 100F;
                blocos[i] = new TextBlock(_Colunas[i].Item1, _Fonte) { Width = w - 0.5F, X = cx, Y = y, AlinhamentoHorizontal = _Colunas[i].Item3 };
                cx += w;
            }

            return blocos;
        }

        public override float Altura(float largura) => Math.Max(_Fonte.AlturaLinha, Criar(0, 0, largura).Max(b => String.IsNullOrEmpty(b.Text) ? 0 : b.Height));

        public override void Desenhar(Gfx gfx, float x, float y, float largura)
        {
            foreach (var b in Criar(x, y, largura))
                b.Draw(gfx);
        }
    }

    /// <summary>
    /// Linha tracejada que separa as divisões do DANFE NFC-e.
    /// </summary>
    internal class SeparadorCupom : ItemCupom
    {
        public override float Altura(float largura) => 2.5F;

        public override void Desenhar(Gfx gfx, float x, float y, float largura)
        {
            gfx.BeginLocalState();
            gfx.SetLineWidth(0.5F);
            gfx.SetLineDash(2, 1.5);
            gfx.DrawLine(new PointF(x, y + 1.25F), new PointF(x + largura, y + 1.25F));
            gfx.Stroke();
            gfx.EndLocalState();
        }
    }

    /// <summary>
    /// Espaço vertical em branco.
    /// </summary>
    internal class EspacoCupom : ItemCupom
    {
        private readonly float _Altura;

        public EspacoCupom(float altura)
        {
            _Altura = altura;
        }

        public override float Altura(float largura) => _Altura;

        public override void Desenhar(Gfx gfx, float x, float y, float largura) { }
    }

    /// <summary>
    /// QR Code centralizado.
    /// </summary>
    internal class QrCodeCupom : ItemCupom
    {
        private readonly QrCode _QrCode;

        public QrCodeCupom(String texto, float lado)
        {
            _QrCode = new QrCode(texto, lado);
        }

        public override float Altura(float largura) => _QrCode.Lado;

        public override void Desenhar(Gfx gfx, float x, float y, float largura)
        {
            _QrCode.SetPosition(x + (largura - _QrCode.Lado) / 2F, y);
            _QrCode.Draw(gfx);
        }
    }

    /// <summary>
    /// Logotipo centralizado, com altura máxima.
    /// </summary>
    internal class LogoCupom : ItemCupom
    {
        private readonly PdfSharp.Drawing.XImage _Logo;
        private readonly float _AlturaMaxima;

        public LogoCupom(PdfSharp.Drawing.XImage logo, float alturaMaxima)
        {
            _Logo = logo;
            _AlturaMaxima = alturaMaxima;
        }

        public override float Altura(float largura) => Math.Min(_AlturaMaxima, (float)(largura * _Logo.PointHeight / _Logo.PointWidth));

        public override void Desenhar(Gfx gfx, float x, float y, float largura)
        {
            gfx.ShowXObject(_Logo, new RectangleF(x, y, largura, Altura(largura)));
        }
    }

    /// <summary>
    /// Desenha um fundo cinza atrás de outro item (ex.: aviso de ambiente de homologação).
    /// </summary>
    internal class FundoCinzaCupom : ItemCupom
    {
        private readonly ItemCupom _Item;

        public FundoCinzaCupom(ItemCupom item)
        {
            _Item = item ?? throw new ArgumentNullException(nameof(item));
        }

        public override float Altura(float largura) => _Item.Altura(largura);

        public override void Desenhar(Gfx gfx, float x, float y, float largura)
        {
            gfx.BeginLocalState();
            gfx.SetFillColor(0.85, 0.85, 0.85);
            gfx.DrawRectangle(new RectangleF(x, y, largura, Altura(largura)));
            gfx.Fill();
            gfx.EndLocalState();

            _Item.Desenhar(gfx, x, y, largura);
        }
    }
}
