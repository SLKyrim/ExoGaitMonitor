function [positionState,a_hl,a_hr,a_kl,a_kr]=gait_start(gait_length,gait_height)
t=0:0.005:2;%����ʱ��
l1=0.4; l2=0.4;%l1�Ǵ��ȳ��ȣ�l2��С�ȳ���
H=gait_height; %HΪ����
S=gait_length/2; %SΪ����
T=2;   %TΪ��̬���ڳ���
vb=S/(T/2);
positionState = 'start';
for i=1:201
xa(i)=0; %xa,yaΪ�ҽ�λ��
ya(i)=0;
x0(i)=(vb/2)*t(i);%x0,y0Ϊ�Źؽ�λ��
y0(i)=(((l1+l2)-sqrt((l1+l2)^2-(S/2)^2))/2)*sin((2*pi/S)*x0(i)+(pi/2))+(l1+l2+sqrt((l1+l2)^2-(S/2)^2))/2;
xc(i)=vb*t(i);%xc,ycΪ���λ��
yc(i)=(H/2)*sin((2*pi/(S))*xc(i)-(pi/2))+(H/2);
end 
for i=1:201 %�����ϥ�ؽ�λ��
l=sqrt((xc(i)-x0(i))^2+(yc(i)-y0(i))^2);
    theat1=acos((l2^2+l^2-l1^2)/(2*l*l2));
    theat2=atan((y0(i)-yc(i))/(x0(i)-xc(i)));
    if(xc(i)>x0(i))
    x3=real(xc(i)-l2*cos(theat2-theat1));
    x4=real(xc(i)-l2*cos(theat2+theat1));
    y3=real(yc(i)+abs(l2*sin(theat2-theat1)));
    y4=real(yc(i)+abs(l2*sin(theat2+theat1)));
    else
     x3=real(xc(i)+l2*cos(theat2-theat1));
    x4=real(xc(i)+l2*cos(theat2+theat1));
    y3=real(yc(i)+abs(l2*sin(theat2-theat1)));
    y4=real(yc(i)+abs(l2*sin(theat2+theat1)));   
    end
    xd(i)=max(x3,x4);
    yd(i)=max(y3,y4);
end
for i=1:201 %�����ϥ�ؽ�λ��
l=sqrt((xa(i)-x0(i))^2+(ya(i)-y0(i))^2);
    theat1=acos((l2^2+l^2-l1^2)/(2*l*l2));
    theat2=atan((y0(i)-ya(i))/(x0(i)-xa(i)));
    if(xa(i)<x0(i))
    x1=real(xa(i)+l2*cos(theat2-theat1));
    x2=real(xa(i)+l2*cos(theat2+theat1));
    y1=real(ya(i)+abs(l2*sin(theat2-theat1)));
    y2=real(ya(i)+abs(l2*sin(theat2+theat1)));
    else
       x1=real(xa(i)-l2*cos(theat2-theat1));
    x2=real(xa(i)-l2*cos(theat2+theat1));
    y1=real(ya(i)+abs(l2*sin(theat2-theat1)));
    y2=real(ya(i)+abs(l2*sin(theat2+theat1))); 
    end
    xb(i)=max(x1,x2);
    yb(i)=max(y1,y2);
end
for i=1:201
a_hl(i)=atan((xd(i)-x0(i))/(y0(i)-yd(i)));
a_hl(i)=a_hl(i)*180/pi;
end
for i=1:201
a_hr(i)=atan((xb(i)-x0(i))/(y0(i)-yb(i)));
a_hr(i)=a_hr(i)*180/pi;
end
for i=1:201
a_kr(i)=atan((xa(i)-xb(i))/(yb(i)-ya(i)));
a_kr(i)=a_hr(i)-(a_kr(i)*180/pi);
if(a_kr(i)<0)
    a_kr(i)=0;
end
end
for i=1:201
a_kl(i)=atan((xc(i)-xd(i))/(yd(i)-yc(i)));
a_kl(i)=a_hl(i)-(a_kl(i)*180/pi);
if(a_kl(i)<0)
    a_kl(i)=0;
end

end
end
