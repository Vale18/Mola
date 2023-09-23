Asset Store link: http://u3d.as/sae
"Jiggly Bubble" is owned by Moonflower Carnivore (http://u3d.as/cxf) and being sold on Unity Asset Store.
For updated documentation, please refer to Google Docs:
https://docs.google.com/document/d/1TK4q9TffvqxYFQEo5Iui9r5IkH-_BzJhsadJkylfPF8

资源商店链接：http://u3d.as/sae
「弹性泡泡 / Jiggly Bubble」由Moonflower Carnivore (http://u3d.as/cxf) 持有并于Unity官方资源商店发售。
如欲查看最新使用说明请见谷歌文件：
https://docs.google.com/document/d/1TK4q9TffvqxYFQEo5Iui9r5IkH-_BzJhsadJkylfPF8

V1.0 (3rd May 2016, initial release)
V1.1 (5th March 2017, update for Unity 5.5)
V1.2 (April 2017, new prefabs: fizzy cheap, soapy zone, exhaling zone)
V1.3 (January 2018, new mobile variants and all desktop prefabs now use the cubemap materials which may require skybox or reflection probe if the shader is denoted with "skybox")

All bubble effects are in the "Prefabs" folder. To use any effect in your scene, you can drag the prefab into your scene or instantiate the prefab from the script.

Scaling can be done by only modifying the scale values of transform component of the prefab parent, all child objects of that prefab will inherit the scale. If you do not want the effect prefab to inherit transform.scale from any parent object, you need to modify the "scaling mode" of particle system main module of all objects, including child particle systems, from "hierarchy" to "local".

To change the emission area of the particle effect, find (or add if none) the “shape” module in the particle system and change the radius, volume or x/y/z length depending on which shape is chosen. When the shape module is expanded, its outline gizmo is visualized in the scene view, allows intuitive adjustment.

Soapy bubble material uses customized "Particles/Alpha Blended Intensify" shader to boost the color intensity. If it generates undesirable result, you can change the shader to Particles/Additive, Alpha Blended or Multiply (Double). You can also edit the PSD file of the bubble texture in graphic editor like Photoshop which has all the component layers retained for greater customization.

Underwater bullet (for Unity 5.5 only) has triggered an editor bug which is fixed in 5.6. This bug causes particle system to disable interpolation which results in broken trail of its sub emitter. Unity is not going to backport the fix to Unity 5.5, hence this variant does not use collision for the initial reflection towards the undersea side.

Underwater bullet (for Unity 5.6 onward)’s prefab parent should be placed under the water surface and adjusted to face upward. You can expand its “shape” module to visualize the particle emitter in scene view. When its (unseen) particle is emitter, it should collide with the water surface collider (mostly plane collider), the collision will kill the initial unseen particle and give birth to “SubEmitter Collision main” which actually generates visible head bubbles, its direction is reflected back to the underwater side, subsequently gives birth to smaller bubble trail. The whole point of the initial reflection is to avoid the bubbles appear above water surface.

Underwater exhaling periodically uses emission curve, its cycle length can be adjusted in the “duration” parameter in the particle system main module.

If you have any question, please send us email (moonflowercarnivore@gmail.com) or private message to our Facebook group (https://www.facebook.com/MoonflowerCarnivore/). Thank you for purchasing this asset legally.