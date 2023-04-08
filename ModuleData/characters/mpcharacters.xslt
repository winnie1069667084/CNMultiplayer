<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output omit-xml-declaration="yes"/>
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>
  <xsl:template match="NPCCharacter[@id='mp_light_infantry_vlandia_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_shock_infantry_vlandia_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_infantry_vlandia_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_ranged_vlandia_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_ranged_vlandia_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_cavalry_vlandia_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_cavalry_vlandia_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_skirmisher_empire_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_shock_infantry_empire_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_infantry_empire_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_ranged_empire_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_ranged_empire_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_cavalry_empire_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_cavalry_empire_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_infantry_khuzait_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_infantry_khuzait_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_ranged_khuzait_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_ranged_khuzait_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_cavalry_khuzait_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_cavalry_khuzait_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_horse_archer_khuzait_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_infantry_aserai_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_shock_infantry_aserai_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_skirmisher_aserai_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_ranged_aserai_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_ranged_aserai_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_cavalry_aserai_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_cavalry_aserai_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_infantry_sturgia_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_infantry_sturgia_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_shock_infantry_sturgia_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_skirmisher_sturgia_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_ranged_sturgia_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_cavalry_sturgia_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_cavalry_sturgia_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_infantry_battania_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_infantry_battania_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_shock_infantry_battania_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_skirmisher_battania_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_ranged_battania_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_heavy_ranged_battania_hero']"/>
  <xsl:template match="NPCCharacter[@id='mp_light_cavalry_battania_hero']"/>
</xsl:stylesheet>