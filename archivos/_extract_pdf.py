from pypdf import PdfReader

ruta = r"archivos\Mundial Runner - Plan de Trabajo-MateoGimenez-MateoGonzalez.pdf"
lector = PdfReader(ruta)

salida = []
for i, pagina in enumerate(lector.pages):
    salida.append(f"\n===== PAGINA {i + 1} =====")
    salida.append(pagina.extract_text() or "")

texto = "\n".join(salida)
with open(r"archivos\_plan_texto.txt", "w", encoding="utf-8") as f:
    f.write(texto)

print("paginas:", len(lector.pages), "caracteres:", len(texto))
