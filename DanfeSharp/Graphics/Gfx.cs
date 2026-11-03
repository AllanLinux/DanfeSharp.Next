using System.Drawing;
using System;
using System.Collections.Generic;
using PdfSharp.Drawing;

namespace DanfeSharp.Graphics
{
    /// <summary>
    /// Desenha em uma página do PDF com o PDFsharp. As medidas são em mm e a origem fica no canto superior esquerdo.
    /// Retângulos e linhas são acumulados até a chamada de <see cref="Stroke"/> ou <see cref="Fill"/>, como em um path do PDF.
    /// </summary>
    internal class Gfx : IDisposable
    {
        private class Estado
        {
            public double LarguraLinha = 1;
            public double[] Tracejado;
            public XColor CorTraco = XColors.Black;
            public XColor CorPreenchimento = XColors.Black;
            public XGraphicsState EstadoGrafico;

            public Estado Clonar() => (Estado)MemberwiseClone();
        }

        private Estado _Estado = new Estado();
        private readonly Stack<Estado> _Estados = new Stack<Estado>();
        private readonly List<XRect> _Retangulos = new List<XRect>();
        private readonly List<Tuple<XPoint, XPoint>> _Linhas = new List<Tuple<XPoint, XPoint>>();

        public XGraphics XGraphics { get; private set; }

        public Gfx(XGraphics xGraphics)
        {
            XGraphics = xGraphics ?? throw new ArgumentNullException(nameof(xGraphics));
        }

        internal void DrawString(string str, RectangleF rect, Fonte fonte, AlinhamentoHorizontal ah = AlinhamentoHorizontal.Esquerda, AlinhamentoVertical av = AlinhamentoVertical.Topo)
        {
            if (fonte == null) throw new ArgumentNullException(nameof(fonte));
            if (fonte.Tamanho <= 0) throw new ArgumentOutOfRangeException(nameof(fonte));
            CheckRectangle(rect);

            var p = rect.Location;

            if (av == AlinhamentoVertical.Base)
                p.Y = rect.Bottom - fonte.AlturaLinha;
            else if (av == AlinhamentoVertical.Centro)
                p.Y += (rect.Height - fonte.AlturaLinha) / 2F ;

            if (ah == AlinhamentoHorizontal.Direita)
                p.X = rect.Right - fonte.MedirLarguraTexto(str);
            if (ah == AlinhamentoHorizontal.Centro)
                p.X += (rect.Width - fonte.MedirLarguraTexto(str)) / 2F;

            ShowText(str, p, fonte);
        }

        /// <summary>
        /// Escreve o texto com o topo da linha no ponto informado.
        /// </summary>
        public void ShowText(String text, PointF point, Fonte fonte)
        {
            if (fonte == null) throw new ArgumentNullException(nameof(fonte));
            CheckPoint(point);
            if (String.IsNullOrEmpty(text)) return;

            var baseLinha = new PointF(point.X, point.Y + fonte.Ascendente).ToPointMeasure();
            XGraphics.DrawString(text, fonte.FonteInterna, new XSolidBrush(_Estado.CorPreenchimento), baseLinha.X, baseLinha.Y, XStringFormats.BaseLineLeft);
        }

        public void ShowXObject(XImage xobj, RectangleF r)
        {
            if (xobj == null) throw new ArgumentNullException(nameof(xobj));
            CheckRectangle(r);

            SizeF xs = new SizeF((float)xobj.PointWidth, (float)xobj.PointHeight).ToMm();

            // Encaixa a imagem inteira no retângulo, mantendo a proporção, e centraliza.
            float escala = Math.Min(r.Width / xs.Width, r.Height / xs.Height);
            SizeF s = new SizeF(xs.Width * escala, xs.Height * escala);
            PointF p = new PointF(r.X + (r.Width - s.Width) / 2F, r.Y + (r.Height - s.Height) / 2F);

            var destino = new RectangleF(p, s).ToPointMeasure();
            XGraphics.DrawImage(xobj, destino.X, destino.Y, destino.Width, destino.Height);
        }

        public void StrokeRectangle(RectangleF rect, float width)
        {
            SetLineWidth(width);
            DrawRectangle(rect);
            Stroke();
        }

        /// <summary>
        /// Define a largura da linha, em pontos.
        /// </summary>
        public void SetLineWidth(float w)
        {
            if (w < 0) throw new ArgumentOutOfRangeException(nameof(w));
            _Estado.LarguraLinha = w;
        }

        /// <summary>
        /// Define o padrão do tracejado, em pontos.
        /// </summary>
        public void SetLineDash(params double[] padrao) => _Estado.Tracejado = padrao;

        public void SetStrokeColor(double r, double g, double b) => _Estado.CorTraco = Cor(r, g, b);

        public void SetFillColor(double r, double g, double b) => _Estado.CorPreenchimento = Cor(r, g, b);

        private static XColor Cor(double r, double g, double b) => XColor.FromArgb((int)Math.Round(r * 255), (int)Math.Round(g * 255), (int)Math.Round(b * 255));

        public void DrawRectangle(RectangleF rect)
        {
            CheckRectangle(rect);
            var r = rect.ToPointMeasure();
            _Retangulos.Add(new XRect(r.X, r.Y, r.Width, r.Height));
        }

        public void DrawLine(PointF p1, PointF p2)
        {
            var a = p1.ToPointMeasure();
            var b = p2.ToPointMeasure();
            _Linhas.Add(Tuple.Create(new XPoint(a.X, a.Y), new XPoint(b.X, b.Y)));
        }

        /// <summary>
        /// Salva o estado gráfico atual (cores, linha e transformações), como o operador q do PDF.
        /// </summary>
        public void BeginLocalState()
        {
            var salvo = _Estado.Clonar();
            salvo.EstadoGrafico = XGraphics.Save();
            _Estados.Push(salvo);
        }

        /// <summary>
        /// Restaura o estado salvo por <see cref="BeginLocalState"/>.
        /// </summary>
        public void EndLocalState()
        {
            var salvo = _Estados.Pop();
            XGraphics.Restore(salvo.EstadoGrafico);
            salvo.EstadoGrafico = null;
            _Estado = salvo;
        }

        /// <summary>
        /// Gira o desenho 90° no sentido anti-horário: o ponto (x, y) passa a ser desenhado em (y, deslocamentoY - x).
        /// </summary>
        public void Rotacionar90AntiHorario(float deslocamentoY)
        {
            XGraphics.MultiplyTransform(new XMatrix(0, -1, 1, 0, 0, deslocamentoY.ToPoint()));
        }

        /// <summary>
        /// Gira o desenho em torno de um ponto (em mm). Ângulos positivos giram no sentido horário.
        /// </summary>
        public void RotacionarEm(float graus, PointF centro)
        {
            var c = centro.ToPointMeasure();
            XGraphics.RotateAtTransform(graus, new XPoint(c.X, c.Y));
        }

        private void CheckRectangle(RectangleF r)
        {
            if (r.X < 0 || r.Y < 0 || r.Width <= 0 || r.Height <= 0) throw new ArgumentException(nameof(r));
        }

        private void CheckPoint(PointF p)
        {
            if (p.X < 0 || p.Y < 0) throw new ArgumentException(nameof(p));
        }

        private XPen CriarCaneta()
        {
            var pen = new XPen(_Estado.CorTraco, _Estado.LarguraLinha);

            if (_Estado.Tracejado != null && _Estado.LarguraLinha > 0)
            {
                // No PDFsharp o padrão do tracejado é proporcional à largura da linha.
                pen.DashStyle = XDashStyle.Custom;
                pen.DashPattern = Array.ConvertAll(_Estado.Tracejado, v => v / _Estado.LarguraLinha);
            }

            return pen;
        }

        public void Stroke()
        {
            var pen = CriarCaneta();

            foreach (var r in _Retangulos) XGraphics.DrawRectangle(pen, r);
            foreach (var l in _Linhas) XGraphics.DrawLine(pen, l.Item1, l.Item2);

            LimparPath();
        }

        public void Fill()
        {
            var brush = new XSolidBrush(_Estado.CorPreenchimento);

            foreach (var r in _Retangulos) XGraphics.DrawRectangle(brush, r);

            LimparPath();
        }

        private void LimparPath()
        {
            _Retangulos.Clear();
            _Linhas.Clear();
        }

        public void Flush() { }

        public void DrawRectangle(float x, float y, float w, float h) => DrawRectangle(new RectangleF(x, y, w, h));

        public void Dispose() => XGraphics.Dispose();
    }
}
