"""Portable structural checks; this does not replace Android/XAML compilation."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET
root = Path(__file__).resolve().parent
xns = '{http://schemas.microsoft.com/winfx/2009/xaml}'
errors=[]; count=0
for p in root.rglob('*'):
    if any(x in p.parts for x in ('bin','obj')): continue
    if p.suffix in ('.xaml','.xml','.csproj','.svg'):
        try: ET.parse(p); count+=1
        except Exception as ex: errors.append(f'{p.relative_to(root)}: {ex}')
keys=set()
for p in (root/'Resources/Styles').glob('*.xaml'):
    keys.update(n.attrib[xns+'Key'] for n in ET.parse(p).getroot().iter() if xns+'Key' in n.attrib)
base=(root/'ViewModels/BaseViewModel.cs').read_text(encoding='utf8')
for p in (root/'Views').glob('*.xaml'):
    doc=ET.parse(p).getroot()
    def check(node, context):
        context=node.attrib.get(xns+'DataType',context)
        source=base
        if context:
            name=context.split(':')[-1]
            source+='\n'+'\n'.join(f.read_text(encoding='utf8') for d in ('ViewModels','Models') for f in (root/d).glob('*.cs') if re.search(r'\bclass\s+'+re.escape(name)+r'\b',f.read_text(encoding='utf8')))
        for value in node.attrib.values():
            for key in re.findall(r'\{StaticResource\s+(\w+)\}',value):
                if key not in keys: errors.append(f'{p.name}: missing resource {key}')
            for binding in re.findall(r'\{Binding\s+(\w+)',value):
                if context and not re.search(r'\b'+re.escape(binding)+r'\b',source): errors.append(f'{p.name}: unknown binding {context}.{binding}')
        for child in node: check(child,context)
    check(doc,None)
    source=p.with_suffix('.xaml.cs').read_text(encoding='utf8')
    for node in doc.iter():
        for event in ('Clicked','StartInteraction','DragInteraction','EndInteraction','CancelInteraction'):
            if event in node.attrib and node.attrib[event] not in source: errors.append(f'{p.name}: missing handler {node.attrib[event]}')
# MAUI converts SVG sources to .png resource names at build time.
images={p.stem for p in (root/'Resources/Images').glob('*')}
for p in list((root/'Views').glob('*.xaml'))+[root/'AppShell.xaml']:
    for source in re.findall(r'(?:Source|Icon)="([a-z0-9_]+)\.png"',p.read_text(encoding='utf8')):
        if source not in images: errors.append(f'{p.name}: missing image {source}')
release=ET.parse(root/'Platforms/Android/AndroidManifest.Release.xml').getroot()
android='{http://schemas.android.com/apk/res/android}'
if release.find('application').attrib.get(android+'usesCleartextTraffic')!='false': errors.append('Release manifest permits HTTP')
if list(root.rglob('*.apk')): errors.append('An unverified APK was bundled')
if errors:
    print('\n'.join(errors));raise SystemExit(1)
print(f'PASS: {count} XML/SVG/project files, binding/resource/handler references, Release HTTP policy, no stale APK.')
